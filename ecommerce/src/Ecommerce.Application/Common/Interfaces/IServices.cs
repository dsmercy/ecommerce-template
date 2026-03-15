using System.Security.Claims;
using Ecommerce.Domain.Entities;

namespace Ecommerce.Application.Common.Interfaces;

public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken ct = default) where T : class;
    Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken ct = default) where T : class;
    Task RemoveAsync(string key, CancellationToken ct = default);
    Task RemoveByPrefixAsync(string prefix, CancellationToken ct = default);
}

public interface IBlobStorageService
{
    Task<string> UploadAsync(Stream fileStream, string fileName, string contentType, CancellationToken ct = default);
    Task DeleteAsync(string blobUrl, CancellationToken ct = default);
}

public interface ISearchService
{
    Task EnsureIndexSettingsAsync(CancellationToken ct = default);
    Task BulkIndexProductsAsync(IEnumerable<Product> products, CancellationToken ct = default);
    Task IndexProductAsync(Product product, CancellationToken ct = default);
    Task DeleteProductIndexAsync(long productId, CancellationToken ct = default);
    Task<SearchResult> SearchProductsAsync(string query, ProductSearchFilter filter, CancellationToken ct = default);
}

public class SearchResult
{
    public IEnumerable<ProductSearchDto> Items { get; set; } = Enumerable.Empty<ProductSearchDto>();
    public long TotalHits { get; set; }
}

public class ProductSearchDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Brand { get; set; }
    public decimal? BasePrice { get; set; }
    public long? CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public string? Slug { get; set; }
    public string? PrimaryImageUrl { get; set; }
}

public class ProductSearchFilter
{
    public string? Query { get; set; }
    public long? CategoryId { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public interface ITokenService
{
    /// <summary>Generates a short-lived signed JWT access token.</summary>
    string GenerateAccessToken(User user);

    /// <summary>
    /// Generates a cryptographically random opaque refresh token.
    /// Returns the RAW value — never store this directly; hash it first.
    /// </summary>
    string GenerateRefreshToken();

    /// <summary>
    /// Validates an expired access token's signature and returns its principal.
    /// Used during token refresh to confirm the token was legitimately issued
    /// even though its lifetime has elapsed.
    /// </summary>
    System.Security.Claims.ClaimsPrincipal GetPrincipalFromExpiredToken(string accessToken);
}

public interface ICurrentUserService
{
    long? UserId { get; }
    string? Email { get; }
    bool IsAuthenticated { get; }
    bool IsAdmin { get; }
}