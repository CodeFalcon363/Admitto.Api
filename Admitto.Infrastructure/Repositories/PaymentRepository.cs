using Admitto.Core.Data;
using Admitto.Core.Entities;
using Admitto.Infrastructure.Interfaces.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace Admitto.Infrastructure.Repositories
{
    public class PaymentRepository : IPaymentRepository
    {
        private readonly AdmittoDbContext _context;

        public PaymentRepository(AdmittoDbContext context)
        {
            _context = context;
        }

        public async Task<Payment> CreateAsync(Payment payment)
        {
            await _context.Payments.AddAsync(payment);
            await _context.SaveChangesAsync();
            return payment;
        }

        public async Task DeleteAsync(Payment payment)
        {
            _context.Payments.Remove(payment);
            await _context.SaveChangesAsync();
        }

        public async Task<(IEnumerable<Payment>, int totalRecords)> GetAllAsync(int pageNumber, int pageSize)
        {
            var totalCount = await _context.Payments.CountAsync();
            var data = await _context.Payments
                .AsNoTracking()
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            return (data, totalCount);
        }

        public Task<Payment?> GetByReferenceAsync(string reference)
            => _context.Payments.AsNoTracking().FirstOrDefaultAsync(e => e.PaymentReference == reference);

        public Task<Payment?> GetByBookingIdAsync(int bookingId)
            => _context.Payments.AsNoTracking().FirstOrDefaultAsync(e => e.BookingId == bookingId);

        public Task<Payment?> GetByIdAsync(int id)
            => _context.Payments.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);

        public async Task<Payment?> UpdateAsync(Payment payment)
        {
            payment.UpdatedAt = DateTime.UtcNow;
            payment.UpdateCount++;
            _context.Payments.Update(payment);
            await _context.SaveChangesAsync();
            return payment;
        }

        /// <summary>
        /// Atomically creates a payment for a booking under Serializable isolation.
        /// Prevents two concurrent requests from both inserting a payment for the same booking.
        /// Returns (existing, false) if already exists, (new, true) if created.
        /// </summary>
        public async Task<(Payment payment, bool created)> GetOrCreateAsync(int bookingId, Payment newPayment)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);

            var existing = await _context.Payments.FirstOrDefaultAsync(p => p.BookingId == bookingId);
            if (existing != null)
            {
                await transaction.RollbackAsync();
                return (existing, false);
            }

            _context.Payments.Add(newPayment);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return (newPayment, true);
        }
    }
}
