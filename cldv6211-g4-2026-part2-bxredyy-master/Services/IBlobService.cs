namespace EventEase.Services
{
    public interface IBlobService
    {
        // Uploads a file picked from the form and returns its public blob URL.
        Task<string> UploadImageAsync(IFormFile file);

        // Deletes a blob by its URL — used when a venue/event is deleted
        Task DeleteImageAsync(string blobUrl);
    }
}
