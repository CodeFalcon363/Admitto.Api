using Admitto.Core.Constants;
using Admitto.Core.Entities;
using Admitto.Core.Models.Requests.Bookings;
using Admitto.Core.Models.Requests.Events;
using Admitto.Core.Models.Requests.Payments;
using Admitto.Core.Models.Requests.TicketTypes;
using Admitto.Core.Models.Requests;
using Admitto.Core.Models.Responses.Bookings;
using Admitto.Core.Models.Responses.Events;
using Admitto.Core.Models.Responses.Payments;
using Admitto.Core.Models.Responses.TicketTypes;
using Admitto.Core.Models.Responses;
using AutoMapper;

namespace Admitto.Infrastructure.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // Auth
            CreateMap<RegisterUserRequest, User>();
            CreateMap<User, UserResponse>();

            // Events
            CreateMap<CreateEventRequest, Event>()
                .AfterMap((src, dest) => dest.CreatedAt = DateTime.UtcNow);
            CreateMap<UpdateEventRequest, Event>()
                .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember != null));
            CreateMap<Event, EventResponse>().ReverseMap();

            // TicketTypes
            CreateMap<CreateTicketTypeRequest, TicketType>()
                .AfterMap((src, dest) => dest.CreatedAt = DateTime.UtcNow);
            CreateMap<UpdateTicketTypeRequest, TicketType>()
                .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember != null));
            CreateMap<TicketType, TicketTypeResponse>().ReverseMap();

            // Bookings
            CreateMap<CreateBookingRequest, Booking>()
                .AfterMap((src, dest) =>
                {
                    dest.CreatedAt = DateTime.UtcNow;
                    dest.Status = BookingStatus.Pending;
                });
            CreateMap<Booking, BookingResponse>().ReverseMap();
            CreateMap<BookingItem, BookingItemResponse>().ReverseMap();

            // Payments
            CreateMap<InitializePaymentRequest, Payment>()
                .AfterMap((src, dest) =>
                {
                    dest.CreatedAt = DateTime.UtcNow;
                    dest.Status = PaymentStatus.Pending;
                });
            CreateMap<Payment, PaymentResponse>().ReverseMap();
        }
    }
}
