using Admitto.Core.Constants;
using Admitto.Core.Models;
using Admitto.Core.Models.Requests.Bookings;
using Admitto.Core.Models.Responses.Bookings;
using Admitto.Infrastructure.Interfaces.IRepositories;
using Admitto.Infrastructure.Interfaces.IServices;
using AutoMapper;
using Microsoft.Extensions.Logging;

namespace Admitto.Infrastructure.Services
{
    public class BookingService : IBookingService
    {
        private readonly IBookingRepository _bookingRepository;
        private readonly ITicketTypeRepository _ticketTypeRepository;
        private readonly INotificationService _notificationService;
        private readonly IMapper _mapper;
        private readonly ILogger<BookingService> _logger;

        public BookingService(
            IBookingRepository bookingRepository,
            ITicketTypeRepository ticketTypeRepository,
            INotificationService notificationService,
            IMapper mapper,
            ILogger<BookingService> logger)
        {
            _bookingRepository = bookingRepository;
            _ticketTypeRepository = ticketTypeRepository;
            _notificationService = notificationService;
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

            var (created, error) = await _bookingRepository.CreateTransactionalAsync(booking, lineItems);
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
            await _notificationService.SendBookingConfirmationAsync(created.Id);

            return new ApiResponse<BookingResponse>
            {
                Success = true,
                Message = ApiMessages.BookingCreated,
                Data = _mapper.Map<BookingResponse>(created)
            };
        }

        public async Task<ApiResponse<bool>> CancelAsync(int id, Guid? callerUserId = null)
        {
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

            booking.Status = BookingStatus.Canceled;
            await _bookingRepository.UpdateAsync(booking);
            _logger.LogInformation("Booking {BookingId} canceled", id);
            await _notificationService.SendCancellationAsync(id);

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
        /// </summary>
        private async Task<(List<BookingLineItem> Items, string? Error)> ValidateAndBuildLineItemsAsync(
            CreateBookingRequest request)
        {
            var lineItems = new List<BookingLineItem>();

            foreach (var item in request.Items)
            {
                var ticketType = await _ticketTypeRepository.GetByIdAsync(item.TicketTypeId);
                if (ticketType == null)
                    return (lineItems, ApiMessages.TicketTypeNotFound);

                if (ticketType.SaleEndDate.HasValue && ticketType.SaleEndDate < DateTime.UtcNow)
                    return (lineItems, ApiMessages.TicketSalesEnded);

                lineItems.Add(new BookingLineItem(item.TicketTypeId, item.Quantity, ticketType.Price));
            }

            return (lineItems, null);
        }
    }
}
