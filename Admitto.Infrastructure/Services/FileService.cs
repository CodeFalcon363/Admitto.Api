using Admitto.Core.Settings;
using Admitto.Infrastructure.Interfaces.IServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Admitto.Infrastructure.Services
{
    public class FileService : IFileService
    {
        private readonly FileSettings _settings;
        private readonly ILogger<FileService> _logger;

        public FileService(IOptions<FileSettings> settings, ILogger<FileService> logger)
        {
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task<string> SaveAsync(Stream fileStream, string fileName, string folder)
        {
            var uploadFolder = Path.Combine(_settings.UploadPath, folder);
            Directory.CreateDirectory(uploadFolder);

            var uniqueFileName = $"{Guid.NewGuid()}{Path.GetExtension(fileName)}";
            var fullPath = Path.Combine(uploadFolder, uniqueFileName);

            using var stream = new FileStream(fullPath, FileMode.Create);
            await fileStream.CopyToAsync(stream);

            _logger.LogInformation("File saved: {FileName} in {Folder}", uniqueFileName, folder);

            return $"{_settings.BaseUrl}/uploads/{folder}/{uniqueFileName}";
        }

        public void Delete(string fileUrl)
        {
            var relativePath = fileUrl.Replace(_settings.BaseUrl, string.Empty).TrimStart('/');
            var fullPath = Path.Combine(_settings.UploadPath, relativePath.Replace("uploads/", string.Empty));
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
                _logger.LogInformation("File deleted: {FilePath}", fullPath);
            }
            else
            {
                _logger.LogWarning("Delete requested for non-existent file: {FilePath}", fullPath);
            }
        }
    }
}
