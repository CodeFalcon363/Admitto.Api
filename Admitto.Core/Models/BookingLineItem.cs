namespace Admitto.Core.Models
{
    /// <summary>
    /// Carries per-line data needed by BookingRepository.CreateTransactionalAsync.
    /// Separates capacity-decrement input from the BookingItem entity so the repository
    /// owns the full atomic operation without leaking EF entities up the call chain.
    /// </summary>
    public record BookingLineItem(int TicketTypeId, int Quantity, decimal UnitPrice);
}
