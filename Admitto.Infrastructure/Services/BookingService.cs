using Admitto.Core.Constants;
using Admitto.Core.Models;
using Admitto.Core.Models.Requests.Bookings;
using Admitto.Core.Models.Responses.Bookings;
using Admitto.Infrastructure.Data;
using Admitto.Infrastructure.Interfaces.IRepositories;
using Admitto.Infrastructure.Interfaces.IServices;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Admitto.Infrastructure.Services
{
    public class BookingService : IBookingService
    {
        private readonly IBookingRepository _bookingRepository;
        private readonly ITicketTypeRepository _ticketTypeRepository;
        private readonly IOutboxRepository _outbox;
        private readonly IMapper _mapper;
        private readonly ILogger<BookingService> _logger;

        public BookingService(
            IBookingRepository bookingRepository,
            ITicketTypeRepository ticketTypeRepository,
            IOutboxRepository outbox,
            IMapper mapper,
            ILogger<BookingService> logger)
        {
            _bookingRepository = bookingRepository;
            _ticketTypeRepository = ticketTypeRepository;
            _outbox = outbox;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<PagedResponse<BookingResponse>> GetAllAsync(int pageNumber, int pageSize)
        {
            var (data, totalRecords) = await _bookingRepository.GetAllAsync(pageNumber, pageSize);

            return new PagedResponse<BookingResponse>
            {
                Success = true,
                Data = _mapper.Map<IEnumerable<BookingResponse>>(data),
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalRecords = totalRecords
            };
        }

        public async Task<PagedResponse<BookingResponse>> GetAllByUserIdAsync(Guid userId, int pageNumber, int pageSize)
        {
            var (data, totalRecords) = await _bookingRepository.GetAllByUserIdAsync(userId, pageNumber, pageSize);

            return new PagedResponse<BookingResponse>
            {
                Success = true,
                Data = _mapper.Map<IEnumerable<BookingResponse>>(data),
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalRecords = totalRecords
            };
        }

        public async Task<ApiResponse<BookingResponse>> GetByIdAsync(int id, Guid? callerUserId = null)
        {
            var booking = await _bookingRepository.GetByIdAsync(id);
            if (booking == null)
                return new ApiResponse<BookingResponse>
                {
                    Success = false,
                    Message = ApiMessages.BookingNotFound
                };

            if (callerUserId.HasValue && booking.UserId != callerUserId.Value)
                return new ApiResponse<BookingResponse>
                {
                    Success = false,
                    Message = ApiMessages.UnauthorizedAccess
                };

            return new ApiResponse<BookingResponse>
            {
                Success = true,
                Data = _mapper.Map<BookingResponse>(booking)
            };
        }

        public async Task<ApiResponse<BookingResponse>> CreateAsync(CreateBookingRequest request, Guid userId)
        {
            var existingBooking = await _bookingRepository.GetByIdempotencyKeyAsync(request.IdempotencyKey);
            if (existingBooking != null)
            {
                _logger.LogInformation("Duplicate booking request detected for idempotency key {Key}", request.IdempotencyKey);
                return new ApiResponse<BookingResponse>
                {
                    Success = true,
                    Data = _mapper.Map<BookingResponse>(existingBooking)
                };
            }

            // Validate business rules only (no capacity changes here).
            // Capacity decrement + insert happen atomically inside CreateTransactionalAsync.
            var (lineItems, validationError) = await ValidateAndBuildLineItemsAsync(request);
            if (validationError != null)
            {
                _logger.LogWarning("Booking validation failed: {Reason}", validationError);
                return new ApiResponse<BookingResponse>
                {
                    Success = false,
                    Message = validationError
                };
            }

            var booking = _mapper.Map<Core.Entities.Booking>(request);
            booking.UserId = userId;

            (Core.Entities.Booking? created, string? error) result;
            try
            {
                result = await _bookingRepository.CreateTransactionalAsync(booking, lineItems);
            }
            catch (DbUpdateException ex) when (DbExceptionHelper.IsDuplicateKey(ex))
            {
                // Two concurrent requests with the same idempotency key both passed the
                // pre-check. IX_Bookings_IdempotencyKey rejected the second insert — fetch
                // and return the first booking that won the race.
                _logger.LogInformation(
                    "Concurrent duplicate booking detected for idempotency key {Key} — returning existing",
                    request.IdempotencyKey);
                var existing = await _bookingRepository.GetByIdempotencyKeyAsync(request.IdempotencyKey);
                return new ApiResponse<BookingResponse>
                {
                    Success = true,
                    Data = _mapper.Map<BookingResponse>(existing)
                };
            }

            var (created, error) = result;
            if (created == null)
            {
                _logger.LogWarning("Booking creation failed in transaction: {Reason}", error);
                return new ApiResponse<BookingResponse>
                {
                    Success = false,
                    Message = error
                };
            }

            _logger.LogInformation("Booking created: {BookingId} for user {UserId}", created.Id, userId);
            await _outbox.EnqueueAsync(
                OutboxEventTypes.BookingConfirmation,
                JsonConvert.SerializeObject(new { Id = created.Id }));

            return new ApiResponse<BookingResponse>
            {
                Success = true,
                Message = ApiMessages.BookingCreated,
                Data = _mapper.Map<BookingResponse>(created)
            };
        }

        public async Task<ApiResponse<bool>> CancelAsync(int id, Guid? callerUserId = null)
        {
            // Ownership check only — no state changes here.
            var booking = await _bookingRepository.GetByIdAsync(id);
            if (booking == null)
            {
                _logger.LogWarning("Cancel attempted on non-existent booking {BookingId}", id);
                return new ApiResponse<bool>
                {
                    Success = false,
                    Message = ApiMessages.BookingNotFound
                };
            }

            if (callerUserId.HasValue && booking.UserId != callerUserId.Value)
            {
                _logger.LogWarning("User {UserId} attempted to cancel booking {BookingId} they do not own", callerUserId, id);
                return new ApiResponse<bool>
                {
                    Success = false,
                    Message = ApiMessages.UnauthorizedAccess
                };
            }

            // Atomically sets status to Canceled and restores ticket capacity in one transaction.
            // The conditional UPDATE (WHERE Status = Confirmed) prevents double-cancel from
            // restoring capacity twice if two concurrent requests race past the check above.
            var (success, error) = await _bookingRepository.CancelTransactionalAsync(id);
            if (!success)
            {
                _logger.LogWarning("Cancel transaction failed for booking {BookingId}: {Reason}", id, error);
                return new ApiResponse<bool>
                {
                    Success = false,
                    Message = error
                };
            }

            _logger.LogInformation("Booking {BookingId} canceled", id);
            await _outbox.EnqueueAsync(
                OutboxEventTypes.BookingCancellation,
                JsonConvert.SerializeObject(new { Id = id }));

            return new ApiResponse<bool>
            {
                Success = true,
                Message = ApiMessages.BookingCanceled,
                Data = true
            };
        }

        /// <summary>
        /// Validates business rules for each line item (existence, sale window) and returns
        /// the priced line items. Does NOT touch capacity — the repository owns that atomically.
        /// All ticket types are loaded in a single WHERE IN query to avoid N+1.
        /// </summary>
        private async Task<(List<BookingLineItem> Items, string? Error)> ValidateAndBuildLineItemsAsync(
            CreateBookingRequest request)
        {
            var requestedIds = request.Items.Select(i => i.TicketTypeId);
            var ticketMap = await _ticketTypeRepository.GetByIdsAsync(requestedIds);

            var lineItems = new List<BookingLineItem>();
            var now = DateTime.UtcNow;

            foreach (var item in request.Items)
            {
                if (!ticketMap.TryGetValue(item.TicketTypeId, out var ticketType))
                    return (lineItems, ApiMessages.TicketTypeNotFound);

                if (ticketType.SaleEndDate.HasValue && ticketType.SaleEndDate < now)
                    return (lineItems, ApiMessages.TicketSalesEnded);

                lineItems.Add(new BookingLineItem(item.TicketTypeId, item.Quantity, ticketType.Price));
            }

            return (lineItems, null);
        }
    }
}
