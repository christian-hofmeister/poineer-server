using Azure;
using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using POIneer.Server.Application.Catalog;
using POIneer.Server.Domain.Models;

namespace POIneer.Server.Infrastructure.Catalog;

public sealed class BlobCatalogSource(BlobContainerClient container, ILogger<BlobCatalogSource> logger)
    : ICatalogSource
{
    public async Task<CatalogSnapshot> ReadAsync(CancellationToken cancellationToken)
    {
        try
        {
            var regions = CatalogMetadataReader.ReadRegions(await ReadBlobAsync("regions.json", cancellationToken));
            var datasets = new List<PublishedDataset>();
            await foreach (var blob in container.GetBlobsAsync(BlobTraits.None, BlobStates.None, prefix: "geofabrik/",
                cancellationToken: cancellationToken))
            {
                if (!blob.Name.EndsWith("/manifest.json", StringComparison.Ordinal))
                    continue;

                var manifest = await ReadBlobAsync(blob.Name, cancellationToken);
                CatalogMetadataReader.AddManifest(datasets, blob.Name, manifest, logger);
            }
            return new CatalogSnapshot(regions, datasets);
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested && IsReadFailure(exception))
        {
            throw new IOException("Unable to read catalog metadata from Azure Blob Storage.", exception);
        }
    }

    private static bool IsReadFailure(Exception exception) =>
        exception is RequestFailedException or AuthenticationFailedException or HttpRequestException
            or OperationCanceledException ||
        exception is AggregateException aggregate && aggregate.Flatten().InnerExceptions.All(IsReadFailure);

    private async Task<string> ReadBlobAsync(string key, CancellationToken cancellationToken)
    {
        var response = await container.GetBlobClient(key).DownloadContentAsync(cancellationToken);
        return response.Value.Content.ToString().TrimStart('\uFEFF');
    }
}
