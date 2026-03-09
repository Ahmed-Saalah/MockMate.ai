namespace MockMate.Api.Services.StorageService;

public interface IImageStorageService
{
    Task<string> UploadImageAsync(
        IFormFile file,
        string folderName = "mockmate/avatars",
        CancellationToken cancellationToken = default
    );
}
