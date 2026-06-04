using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace EventEase.Services
{
    // POE Part 2A: blob storage service that uploads images to Azurite (venue-images container).
    public class BlobService : IBlobService
    {
        private readonly string _connectionString;
        private readonly string _containerName;
        private BlobContainerClient? _containerClient;

        public BlobService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("AzureBlobStorage")
                ?? "UseDevelopmentStorage=true";
            _containerName = configuration["AzureBlob:ContainerName"] ?? "venue-images";
        }

        private async Task<BlobContainerClient> GetContainerAsync()
        {
            if (_containerClient != null) return _containerClient;

            // Fail fast when Azurite is offline instead of hanging.
            var options = new BlobClientOptions();
            options.Retry.MaxRetries = 1;
            options.Retry.NetworkTimeout = TimeSpan.FromSeconds(8);

            var blobServiceClient = new BlobServiceClient(_connectionString, options);
            var client = blobServiceClient.GetBlobContainerClient(_containerName);
            await client.CreateIfNotExistsAsync(PublicAccessType.Blob);
            _containerClient = client;
            return _containerClient;
        }

        // POE Part 2A: upload image to Azurite and return its URL.
        public async Task<string> UploadImageAsync(IFormFile file)
        {
            try
            {
                var container = await GetContainerAsync();
                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file!.FileName)}";
                var blobClient = container.GetBlobClient(fileName);

                using var ms = new MemoryStream();
                await file.CopyToAsync(ms);
                ms.Position = 0;

                await blobClient.UploadAsync(ms, new BlobHttpHeaders { ContentType = file.ContentType });
                return blobClient.Uri.ToString();
            }
            catch (Exception ex)
            {
                throw new Exception($"Azurite upload failed: {ex.Message}", ex);
            }
        }

        // POE Part 2A: delete an image from Azurite.
        public async Task DeleteImageAsync(string blobUrl)
        {
            if (string.IsNullOrWhiteSpace(blobUrl)) return;

            try
            {
                var container = await GetContainerAsync();
                var uri = new Uri(blobUrl);
                var blobName = Path.GetFileName(uri.LocalPath);
                var blobClient = container.GetBlobClient(blobName);
                await blobClient.DeleteIfExistsAsync();
            }
            catch
            {
                // ignore if Azurite is offline
            }
        }
    }
}
