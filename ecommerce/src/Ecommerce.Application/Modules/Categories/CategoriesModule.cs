using Ecommerce.Application.Common.Models;
using Ecommerce.Domain.Entities;
using Ecommerce.Domain.Interfaces;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Application.Modules.Categories;

public record CreateCategoryRequest(string Name, long? ParentId);
public record UpdateCategoryRequest(string? Name, long? ParentId);

public class CategoryResponse
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Slug { get; set; }
    public long? ParentId { get; set; }
    public string? ParentName { get; set; }
    public List<CategoryResponse> Children { get; set; } = new();
}

public class CreateCategoryValidator : AbstractValidator<CreateCategoryRequest>
{
    public CreateCategoryValidator() => RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
}

public interface ICategoryService
{
    Task<ApiResponse<IEnumerable<CategoryResponse>>> GetAllAsync(CancellationToken ct = default);
    Task<ApiResponse<CategoryResponse>> GetByIdAsync(long id, CancellationToken ct = default);
    Task<ApiResponse<CategoryResponse>> CreateAsync(CreateCategoryRequest request, CancellationToken ct = default);
    Task<ApiResponse<CategoryResponse>> UpdateAsync(long id, UpdateCategoryRequest request, CancellationToken ct = default);
    Task<ApiResponse<bool>> DeleteAsync(long id, CancellationToken ct = default);
}

public class CategoryService : ICategoryService
{
    private readonly IRepository<Category> _repo;
    private readonly IUnitOfWork _uow;

    public CategoryService(IRepository<Category> repo, IUnitOfWork uow) { _repo = repo; _uow = uow; }

    public async Task<ApiResponse<IEnumerable<CategoryResponse>>> GetAllAsync(CancellationToken ct = default)
    {
        var cats = await _repo.Query().Include(c => c.Children).Include(c => c.Parent).ToListAsync(ct);
        return ApiResponse<IEnumerable<CategoryResponse>>.Ok(cats.Where(c => c.ParentId == null).Select(MapResponse));
    }

    public async Task<ApiResponse<CategoryResponse>> GetByIdAsync(long id, CancellationToken ct = default)
    {
        var cat = await _repo.Query().Include(c => c.Children).Include(c => c.Parent).FirstOrDefaultAsync(c => c.Id == id, ct);
        return cat is null ? ApiResponse<CategoryResponse>.Fail("Category not found.") : ApiResponse<CategoryResponse>.Ok(MapResponse(cat));
    }

    public async Task<ApiResponse<CategoryResponse>> CreateAsync(CreateCategoryRequest request, CancellationToken ct = default)
    {
        var slug = request.Name.ToLowerInvariant().Replace(" ", "-");
        var cat = new Category { Name = request.Name, Slug = slug, ParentId = request.ParentId };
        await _repo.AddAsync(cat, ct);
        await _uow.SaveChangesAsync(ct);
        return ApiResponse<CategoryResponse>.Ok(MapResponse(cat), "Category created.");
    }

    public async Task<ApiResponse<CategoryResponse>> UpdateAsync(long id, UpdateCategoryRequest request, CancellationToken ct = default)
    {
        var cat = await _repo.GetByIdAsync(id, ct);
        if (cat is null) return ApiResponse<CategoryResponse>.Fail("Category not found.");
        if (request.Name is not null) { cat.Name = request.Name; cat.Slug = request.Name.ToLowerInvariant().Replace(" ", "-"); }
        if (request.ParentId.HasValue) cat.ParentId = request.ParentId;
        _repo.Update(cat);
        await _uow.SaveChangesAsync(ct);
        return ApiResponse<CategoryResponse>.Ok(MapResponse(cat), "Category updated.");
    }

    public async Task<ApiResponse<bool>> DeleteAsync(long id, CancellationToken ct = default)
    {
        var cat = await _repo.GetByIdAsync(id, ct);
        if (cat is null) return ApiResponse<bool>.Fail("Category not found.");
        _repo.Remove(cat);
        await _uow.SaveChangesAsync(ct);
        return ApiResponse<bool>.Ok(true, "Category deleted.");
    }

    private static CategoryResponse MapResponse(Category c) => new()
    {
        Id = c.Id, Name = c.Name, Slug = c.Slug, ParentId = c.ParentId, ParentName = c.Parent?.Name,
        Children = c.Children.Select(MapResponse).ToList()
    };
}
