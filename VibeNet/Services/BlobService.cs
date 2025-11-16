using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Options;
using SixLabors.ImageSharp.Formats.Jpeg;
using VibeNet.Config;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace VibeNet.Services
{
    public class BlobService
    {
        private readonly BlobContainerClient _container;
        private readonly string _finalUrlPrefix;
        private readonly int _maxFileSizeBytes;
        private readonly int _jpegQuality;
        public BlobService(IOptions<AzureSettings> settings, IOptions<ProfilePictureSettings> picSettings)
        {
            var cfg = settings.Value;

            _container = new BlobContainerClient(cfg.BlobConnectionString, cfg.ImagesContainer);
            _container.CreateIfNotExists(PublicAccessType.Blob);

            _finalUrlPrefix = $"{cfg.BaseUrl}/{cfg.ImagesContainer}/";

            _maxFileSizeBytes = picSettings.Value.MaxFileSizeMB * 1024 * 1024;
            _jpegQuality = picSettings.Value.JpegQuality;
        }
        private static readonly HashSet<string> AllowedContentTypes = new()
        {
            "image/jpeg",
            "image/jpg",
            "image/png"
        };
        public async Task<string> UploadProfilePictureAsync(Guid userId, IFormFile file)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("Invalid file.");

            if (file.Length > _maxFileSizeBytes)
                throw new ArgumentException("File exceeds 5 MB limit.");

            if (!AllowedContentTypes.Contains(file.ContentType.ToLower()))
                throw new ArgumentException("Only JPEG/PNG files allowed.");

            using var inputStream = file.OpenReadStream();
            using var image = await Image.LoadAsync(inputStream);
            using var outStream = new MemoryStream();
            var encoder = new JpegEncoder { Quality = _jpegQuality };
            await image.SaveAsJpegAsync(outStream, encoder);
            outStream.Position = 0;

            var blobName = $"{userId}.jpg";
            var blobClient = _container.GetBlobClient(blobName);

            var httpHeaders = new BlobHttpHeaders { ContentType = "image/jpeg" };

            await blobClient.UploadAsync(outStream, true);
            await blobClient.SetHttpHeadersAsync(httpHeaders);

            return $"{_finalUrlPrefix}{blobName}";
        }
    }
}
