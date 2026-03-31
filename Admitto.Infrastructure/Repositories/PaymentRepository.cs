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
        public async Task<bool> AnyAsync(int id)
        {
            var response = await _context.Payments.AnyAsync(e => e.Id == id);
            return response;
        }

        public async Task<Payment> CreateAsync(Payment payment)
        {
            await _context.Payments.AddAsync(payment);
            await _context.SaveChangesAsync();
            return payment;
        }

        public async Task DeleteAsync(Payment payment)
        {
            var response = await _context.Payments.AnyAsync(e => e.Id == payment.Id);
            if(response)
            {
                _context.Payments.Remove(payment);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<(IEnumerable<Payment>, int totalRecords)> GetAllAsync(int pageNumber, int pageSize)
        {
            var totalCount = await _context.Payments.CountAsync();
            var data = await _context.Payments
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            return (data, totalCount);
        }

        public async Task<Payment?> GetByReferenceAsync(string reference)
        {
            return await _context.Payments.FirstOrDefaultAsync(e => e.PaymentReference == reference);
        }

        public async Task<Payment?> GetByBookingIdAsync(int bookingId)
        {
            var response = await _context.Payments.FirstOrDefaultAsync(e => e.BookingId == bookingId);
            if (response == null)
                return null;
            return response;
        }

        public async Task<Payment?> GetByIdAsync(int id)
        {
            var response = await _context.Payments.FindAsync(id);
            if(response == null)
                return null;
            return response;
        }

        public async Task<Payment?> UpdateAsync(Payment payment)
        {
            var response = await _context.Payments.AnyAsync(e => e.Id == payment.Id);
            if (response == false)
                return null;
            _context.Payments.Update(payment);
            await _context.SaveChangesAsync();
            return payment;

        }
    }
}
