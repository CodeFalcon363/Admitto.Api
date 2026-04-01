using Admitto.Core.Settings;
using Admitto.Infrastructure.Interfaces.IServices;
using Microsoft.Extensions.Options;

namespace Admitto.Infrastructure.Services
{
    public class FileService : IFileService
    {
        private readonly FileSettings _settings;

        public FileService(IOptions<FileSettings> settings)
        {
            _settings = settings.Value;
        }

        public async Task<string> SaveAsync(Stream fileStream, string fileName, string folder)
        {
            var uploadFolder = Path.Combine(_settings.UploadPath, folder);
            Directory.CreateDirectory(uploadFolder);

            var uniqueFileName = $"{Guid.NewGuid()}{Path.GetExtension(fileName)}";
            var fullPath = Path.Combine(uploadFolder, uniqueFileName);

            using var stream = new FileStream(fullPath, FileMode.Create);
            await fileStream.CopyToAsync(stream);

            return $"{_settings.BaseUrl}/uploads/{folder}/{uniqueFileName}";
        }

        public void Delete(string fileUrl)
        {
            var relativePath = fileUrl.Replace(_settings.BaseUrl, string.Empty).TrimStart('/');
            var fullPath = Path.Combine(_settings.UploadPath, relativePath.Replace("uploads/", string.Empty));
            if (File.Exists(fullPath))
                File.Delete(fullPath);
        }
    }
}
