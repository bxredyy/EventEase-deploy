using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace EventEase.Services
{
    // POE Part 2A: Concrete blob storage implementation that talks to the
    //              Azurite Emulator (NOT live Azure — that's Part 3).
    //              Reads "UseDevelopmentStorage=true" from appsettings.json
    //              and uploads images to the "venue-images" container.
    public class BlobService : IBlobService
    {
        private readonly string _connectionString;
        private readonly string _containerName;

        // We cache the container client after the first call so we don't
        // re-create it on every upload (small performance win).
        private BlobContainerClient? _containerClient;

        public BlobService(IConfiguration configuration)
        {
            // The "??" operator picks the right side if the left is null.
            // So if appsettings.json is missing the key, we still default
            // to the local Azurite emulator
            _connectionString = configuration.GetConnectionString("AzureBlobStorage")
                ?? "UseDevelopmentStorage=true";
            _containerName = configuration["AzureBlob:ContainerName"] ?? "venue-images";
        }

        // Lazy initialisation: we only connect to Azurite the FIRST time
        // someone uploads or deletes. Reason — when Azurite isn't running,
        // the rest of the app (Venues list, Events list, etc.) still loads
        // CreateIfNotExistsAsync makes the "venue-images" container if it
        // doesn't exist, so the marker doesn't have to create it manually
        private async Task<BlobContainerClient> GetContainerAsync()
        {
            if (_containerClient != null) return _containerClient;

            var blobServiceClient = new BlobServiceClient(_connectionString);
            var client = blobServiceClient.GetBlobContainerClient(_containerName);
            await client.CreateIfNotExistsAsync(PublicAccessType.Blob);
            _containerClient = client;
            return _containerClient;
        }

        // POE Part 2A: Uploads the file to Azurite and returns its URL
        //              Wrapped in a try/catch at the SERVICE level so that
        //              even if Azurite is down or unreachable, we throw a
        //              clean managed Exception that the controller can
        //              catch
        public async Task<string> UploadImageAsync(IFormFile file)
        {
            Console.WriteLine($"[BlobService] >>> UploadImageAsync START: {file?.FileName} ({file?.Length} bytes)");
            try
            {
                Console.WriteLine("[BlobService] Step 1: GetContainerAsync");
                var container = await GetContainerAsync();
                Console.WriteLine("[BlobService] Step 2: container ready");

                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file!.FileName)}";
                var blobClient = container.GetBlobClient(fileName);
                Console.WriteLine($"[BlobService] Step 3: blob client created -> {fileName}");

                // Read the IFormFile into a MemoryStream first
                using var ms = new MemoryStream();
                await file.CopyToAsync(ms);
                ms.Position = 0;
                Console.WriteLine($"[BlobService] Step 4: file copied to MemoryStream ({ms.Length} bytes)");

                await blobClient.UploadAsync(ms, new BlobHttpHeaders
                {
                    ContentType = file.ContentType
                });
                Console.WriteLine("[BlobService] Step 5: UploadAsync completed");

                var url = blobClient.Uri.ToString();
                Console.WriteLine($"[BlobService] <<< Returning URL: {url}");
                return url;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[BlobService] !!! ERROR: {ex.GetType().Name}: {ex.Message}");
                Console.WriteLine($"[BlobService] !!! INNER: {ex.InnerException?.Message}");
                throw new Exception($"Azurite upload failed: {ex.Message}", ex);
            }
        }

        // Cleans up an old blob when the venue/event is deleted or its
        // image is being replaced. The try/catch means a failed delete
        // never breaks the user's main action
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
                // If Azurite is down, skip silently 
            }
        }
    }
}
