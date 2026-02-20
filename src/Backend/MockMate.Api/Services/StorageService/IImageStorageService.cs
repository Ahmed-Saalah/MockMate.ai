namespace MockMate.Api.Services.StorageService;

public interface IImageStorageService
{
    // Takes a file, returns the public URL
    Task<string> UploadImageAsync(
        IFormFile file,
        string folderName = "mockmate/avatars",
        CancellationToken cancellationToken = default
    );
}
