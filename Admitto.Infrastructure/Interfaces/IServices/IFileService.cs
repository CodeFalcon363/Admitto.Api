namespace Admitto.Infrastructure.Interfaces.IServices
{
    public interface IFileService
    {
        Task<string> SaveAsync(Stream fileStream, string fileName, string folder);
        Task DeleteAsync(string fileUrl);
    }
}
