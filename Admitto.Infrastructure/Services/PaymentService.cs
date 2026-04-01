using Admitto.Core.Constants;
using Admitto.Core.Entities;
using Admitto.Core.Models;
using Admitto.Core.Models.Requests.Payments;
using Admitto.Core.Models.Responses.Payments;
using Admitto.Infrastructure.Interfaces.IRepositories;
using Admitto.Infrastructure.Interfaces.IServices;
using AutoMapper;
using Microsoft.Extensions.Logging;

namespace Admitto.Infrastructure.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly IPaymentRepository _paymentRepository;
        private readonly IBookingRepository _bookingRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<PaymentService> _logger;

        public PaymentService(IPaymentRepository paymentRepository, IBookingRepository bookingRepository, IMapper mapper, ILogger<PaymentService> logger)
        {
            _paymentRepository = paymentRepository;
            _bookingRepository = bookingRepository;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<ApiResponse<PaymentResponse>> InitializeAsync(InitializePaymentRequest request)
        {
            var booking = await _bookingRepository.GetByIdAsync(request.BookingId);
            if (booking == null)
                return new ApiResponse<PaymentResponse>
                {
                    Success = false,
                    Message = ApiMessages.BookingNotFound
                };

            // Serializable transaction prevents two concurrent requests from both inserting
            // a payment for the same booking — one wins, the other gets the existing record.
            var (payment, created) = await _paymentRepository.GetOrCreateAsync(
                request.BookingId,
                MapToPaymentEntity(request, booking.UserId));

            if (!created)
            {
                _logger.LogWarning("Duplicate payment initialization for booking {BookingId}", request.BookingId);
                return new ApiResponse<PaymentResponse>
                {
                    Success = false,
                    Message = ApiMessages.PaymentAlreadyProcessed
                };
            }

            _logger.LogInformation("Payment initialized: {PaymentId} for booking {BookingId}", payment.Id, request.BookingId);

            return new ApiResponse<PaymentResponse>
            {
                Success = true,
                Data = _mapper.Map<PaymentResponse>(payment)
            };
        }

        public async Task<ApiResponse<PaymentResponse>> VerifyAsync(string reference)
        {
            var payment = await _paymentRepository.GetByReferenceAsync(reference);
            if (payment == null)
            {
                _logger.LogWarning("Payment not found for reference {Reference}", reference);
                return new ApiResponse<PaymentResponse>
                {
                    Success = false,
                    Message = ApiMessages.PaymentNotFound
                };
            }

            _logger.LogInformation("Payment verified: {PaymentId}", payment.Id);

            return new ApiResponse<PaymentResponse>
            {
                Success = true,
                Message = ApiMessages.PaymentProcessed,
                Data = _mapper.Map<PaymentResponse>(payment)
            };
        }

        public async Task<ApiResponse<PaymentResponse>> GetByIdAsync(int id)
        {
            var payment = await _paymentRepository.GetByIdAsync(id);
            if (payment == null)
                return new ApiResponse<PaymentResponse>
                {
                    Success = false,
                    Message = ApiMessages.PaymentNotFound
                };

            return new ApiResponse<PaymentResponse>
            {
                Success = true,
                Data = _mapper.Map<PaymentResponse>(payment)
            };
        }

        private Payment MapToPaymentEntity(InitializePaymentRequest request, Guid userId)
        {
            var payment = _mapper.Map<Payment>(request);
            payment.UserId = userId;
            payment.PaymentReference = Guid.NewGuid().ToString();
            return payment;
        }
    }
}
