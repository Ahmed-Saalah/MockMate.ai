using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Options;
using MockMate.Api.Configuration;

namespace MockMate.Api.Services.StorageService;

public sealed class CloudinaryStorageService : IImageStorageService
{
    private readonly Cloudinary _cloudinary;

    public CloudinaryStorageService(IOptions<CloudinaryOptions> options)
    {
        var settings = options.Value;
        var account = new Account(settings.CloudName, settings.ApiKey, settings.ApiSecret);
        _cloudinary = new Cloudinary(account);
    }

    public async Task<string> UploadImageAsync(
        IFormFile file,
        string folderName = "mockmate/avatars",
        CancellationToken cancellationToken = default
    )
    {
        if (file.Length == 0)
            return string.Empty;

        using var stream = file.OpenReadStream();

        var uploadParams = new ImageUploadParams
        {
            File = new FileDescription(file.FileName, stream),
            Folder = folderName,

            Transformation = new Transformation()
                .Width(500)
                .Height(500)
                .Crop("fill")
                .Gravity("face"),
        };

        var uploadResult = await _cloudinary.UploadAsync(uploadParams, cancellationToken);

        if (uploadResult.Error != null)
        {
            throw new Exception($"Cloudinary upload failed: {uploadResult.Error.Message}");
        }

        return uploadResult.SecureUrl.ToString();
    }
}
