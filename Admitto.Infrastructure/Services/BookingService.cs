using Admitto.Core.Constants;
using Admitto.Core.Entities;
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

        public BookingService(IBookingRepository bookingRepository, ITicketTypeRepository ticketTypeRepository, INotificationService notificationService, IMapper mapper, ILogger<BookingService> logger)
        {
            _bookingRepository = bookingRepository;
            _ticketTypeRepository = ticketTypeRepository;
            _notificationService = notificationService;
            _mapper = mapper;
            _logger = logger;
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

        public async Task<ApiResponse<BookingResponse>> GetByIdAsync(int id)
        {
            var booking = await _bookingRepository.GetByIdAsync(id);
            if (booking == null)
                return new ApiResponse<BookingResponse> { Success = false, Message = ApiMessages.BookingNotFound };

            return new ApiResponse<BookingResponse> { Success = true, Data = _mapper.Map<BookingResponse>(booking) };
        }

        public async Task<ApiResponse<BookingResponse>> CreateAsync(CreateBookingRequest request)
        {
            var existingBooking = await _bookingRepository.GetByIdempotencyKeyAsync(request.IdempotencyKey);
            if (existingBooking != null)
            {
                _logger.LogInformation("Duplicate booking request detected for idempotency key {Key}", request.IdempotencyKey);
                return new ApiResponse<BookingResponse> { Success = true, Data = _mapper.Map<BookingResponse>(existingBooking) };
            }

            var validationResult = await ValidateAndBuildItems(request);
            if (validationResult.Error != null)
            {
                _logger.LogWarning("Booking validation failed: {Reason}", validationResult.Error);
                return new ApiResponse<BookingResponse> { Success = false, Message = validationResult.Error };
            }

            var created = await _bookingRepository.CreateAsync(_mapper.Map<Booking>(request), validationResult.Items);
            _logger.LogInformation("Booking created: {BookingId}", created.Id);
            await _notificationService.SendBookingConfirmationAsync(created.Id);
            return new ApiResponse<BookingResponse>
            {
                Success = true,
                Message = ApiMessages.BookingCreated,
                Data = _mapper.Map<BookingResponse>(created)
            };
        }

        public async Task<ApiResponse<bool>> CancelAsync(int id)
        {
            var booking = await _bookingRepository.GetByIdAsync(id);
            if (booking == null)
            {
                _logger.LogWarning("Cancel attempted on non-existent booking {BookingId}", id);
                return new ApiResponse<bool> { Success = false, Message = ApiMessages.BookingNotFound };
            }

            await _bookingRepository.UpdateAsync(ApplyCancel(booking));
            _logger.LogInformation("Booking {BookingId} canceled", id);
            await _notificationService.SendCancellationAsync(id);
            return new ApiResponse<bool> { Success = true, Message = ApiMessages.BookingCanceled, Data = true };
        }

        private Booking ApplyCancel(Booking booking)
        {
            booking.Status = BookingStatus.Canceled;
            return booking;
        }

        private async Task<(List<BookingItem> Items, string? Error)> ValidateAndBuildItems(CreateBookingRequest request)
        {
            var items = new List<BookingItem>();
            foreach (var item in request.Items)
            {
                var ticketType = await _ticketTypeRepository.GetByIdAsync(item.TicketTypeId);
                if (ticketType == null)
                    return (items, ApiMessages.TicketTypeNotFound);

                if (ticketType.Capacity == 0)
                    return (items, ApiMessages.InsufficientCapacity);

                if (ticketType.Capacity < item.Quantity)
                    return (items, ApiMessages.InsufficientTickets);

                if (ticketType.SaleEndDate.HasValue && ticketType.SaleEndDate < DateTime.UtcNow)
                    return (items, ApiMessages.TicketSalesEnded);

                items.Add(BuildBookingItem(item.TicketTypeId, item.Quantity, ticketType.Price));

                ticketType.Capacity -= item.Quantity;
                await _ticketTypeRepository.UpdateAsync(ticketType);
            }
            return (items, null);
        }

        private BookingItem BuildBookingItem(int ticketTypeId, int quantity, decimal unitPrice)
        {
            return new BookingItem
            {
                TicketTypeId = ticketTypeId,
                Quantity = quantity,
                UnitPrice = unitPrice,
                CreatedAt = DateTime.UtcNow
            };
        }
    }
}
