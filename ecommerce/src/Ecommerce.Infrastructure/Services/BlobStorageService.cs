using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Ecommerce.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Ecommerce.Infrastructure.Services;

public class BlobStorageService : IBlobStorageService
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly string _containerName;
    private readonly ILogger<BlobStorageService> _logger;

    public BlobStorageService(IConfiguration config, ILogger<BlobStorageService> logger)
    {
        var connectionString = config["AzureBlobStorage:ConnectionString"]
            ?? throw new InvalidOperationException("Azure Blob Storage connection string not configured.");
        _containerName = config["AzureBlobStorage:ContainerName"] ?? "ecommerce-media";
        _blobServiceClient = new BlobServiceClient(connectionString);
        _logger = logger;
    }

    public async Task<string> UploadAsync(Stream fileStream, string fileName, string contentType, CancellationToken ct = default)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
        await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob, cancellationToken: ct);

        var blobClient = containerClient.GetBlobClient(fileName);
        await blobClient.UploadAsync(fileStream, new BlobHttpHeaders { ContentType = contentType }, cancellationToken: ct);

        _logger.LogInformation("Blob uploaded: {BlobName}", fileName);
        return blobClient.Uri.ToString();
    }

    public async Task DeleteAsync(string blobUrl, CancellationToken ct = default)
    {
        try
        {
            var uri = new Uri(blobUrl);
            var blobName = uri.AbsolutePath.TrimStart('/').Replace($"{_containerName}/", "");
            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            var blobClient = containerClient.GetBlobClient(blobName);
            await blobClient.DeleteIfExistsAsync(cancellationToken: ct);
            _logger.LogInformation("Blob deleted: {BlobUrl}", blobUrl);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete blob: {BlobUrl}", blobUrl);
        }
    }
}
