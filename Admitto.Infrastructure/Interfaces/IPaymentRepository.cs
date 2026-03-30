using Admitto.Core.Entities;

namespace Admitto.Infrastructure.Interfaces
{
    public interface IPaymentRepository
    {
        Task<(IEnumerable<Payment>, int totalRecords)> GetAllAsync(int pageNumber, int pageSize);
        Task<Payment?> GetByBookingIdAsync(int bookingId);
        Task<Payment?> GetByIdAsync(int id);
        Task<Payment> CreateAsync(Payment payment);
        Task<Payment?> UpdateAsync(Payment payment);
        Task<bool> AnyAsync(int id);
        Task DeleteAsync(Payment payment);
    }
}
