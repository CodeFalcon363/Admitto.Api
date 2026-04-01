using Admitto.Core.Settings;
using Admitto.Infrastructure.Interfaces.IServices;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Admitto.Infrastructure.Services
{
    public class S3FileService : IFileService
    {
        private readonly IAmazonS3 _s3Client;
        private readonly S3Settings _settings;
        private readonly ILogger<S3FileService> _logger;

        public S3FileService(IAmazonS3 s3Client, IOptions<S3Settings> settings, ILogger<S3FileService> logger)
        {
            _s3Client = s3Client;
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task<string> SaveAsync(Stream fileStream, string fileName, string folder)
        {
            var key = $"{folder}/{Guid.NewGuid()}{Path.GetExtension(fileName)}";

            var request = new PutObjectRequest
            {
                BucketName = _settings.BucketName,
                Key = key,
                InputStream = fileStream,
                AutoCloseStream = false
            };

            await _s3Client.PutObjectAsync(request);

            _logger.LogInformation("File uploaded to S3: {Key}", key);

            return $"{_settings.BaseUrl}/{key}";
        }

        public async Task DeleteAsync(string fileUrl)
        {
            var key = fileUrl.Replace($"{_settings.BaseUrl}/", string.Empty);

            await _s3Client.DeleteObjectAsync(_settings.BucketName, key);

            _logger.LogInformation("File deleted from S3: {Key}", key);
        }
    }
}
