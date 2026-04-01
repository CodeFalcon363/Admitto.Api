using Admitto.Core.Constants;
using Admitto.Core.Entities;
using Admitto.Core.Models;
using Admitto.Core.Models.Requests.Events;
using Admitto.Core.Models.Responses.Events;
using Admitto.Infrastructure.Interfaces.IRepositories;
using Admitto.Infrastructure.Interfaces.IServices;
using AutoMapper;
using Microsoft.Extensions.Logging;

namespace Admitto.Infrastructure.Services
{
    public class EventService : IEventService
    {
        private readonly IEventRepository _eventRepository;
        private readonly INotificationService _notificationService;
        private readonly IMapper _mapper;
        private readonly ILogger<EventService> _logger;

        public EventService(IEventRepository eventRepository, INotificationService notificationService, IMapper mapper, ILogger<EventService> logger)
        {
            _eventRepository = eventRepository;
            _notificationService = notificationService;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<PagedResponse<EventResponse>> GetAllAsync(int pageNumber, int pageSize)
        {
            var (data, totalRecords) = await _eventRepository.GetAllAsync(pageNumber, pageSize);
            return new PagedResponse<EventResponse>
            {
                Success = true,
                Data = _mapper.Map<IEnumerable<EventResponse>>(data),
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalRecords = totalRecords
            };
        }

        public async Task<ApiResponse<EventResponse>> GetByIdAsync(int id)
        {
            var ev = await _eventRepository.GetByIdAsync(id);
            if (ev == null)
                return new ApiResponse<EventResponse> { Success = false, Message = ApiMessages.EventNotFound };

            return new ApiResponse<EventResponse> { Success = true, Message = ApiMessages.EventFound, Data = _mapper.Map<EventResponse>(ev) };
        }

        public async Task<ApiResponse<EventResponse>> GetBySlugAsync(string slug)
        {
            var ev = await _eventRepository.GetBySlugAsync(slug);
            if (ev == null)
                return new ApiResponse<EventResponse> { Success = false, Message = ApiMessages.EventNotFound };

            return new ApiResponse<EventResponse> { Success = true, Message = ApiMessages.EventFound, Data = _mapper.Map<EventResponse>(ev) };
        }

        public async Task<ApiResponse<EventResponse>> CreateAsync(CreateEventRequest request)
        {
            var created = await _eventRepository.CreateAsync(MapToEntity(request));
            await _notificationService.SendEventCreatedAsync(created.Id);
            return new ApiResponse<EventResponse>
            {
                Success = true,
                Message = ApiMessages.EventCreated,
                Data = _mapper.Map<EventResponse>(created)
            };
        }

        public async Task<ApiResponse<EventResponse>> UpdateAsync(string slug, UpdateEventRequest request)
        {
            var ev = await _eventRepository.GetBySlugAsync(slug);
            if (ev == null)
                return new ApiResponse<EventResponse> { Success = false, Message = ApiMessages.EventNotFound };

            var updated = await _eventRepository.UpdateAsync(ApplyUpdate(request, ev));
            await _notificationService.SendEventUpdatedAsync(ev.Id);
            return new ApiResponse<EventResponse>
            {
                Success = true,
                Message = ApiMessages.EventUpdated,
                Data = _mapper.Map<EventResponse>(updated)
            };
        }

        public async Task<ApiResponse<bool>> DeleteAsync(int id)
        {
            var ev = await _eventRepository.GetByIdAsync(id);
            if (ev == null)
                return new ApiResponse<bool> { Success = false, Message = ApiMessages.EventNotFound };

            var organizerId = ev.OrganizerId;
            var title = ev.Title;
            await _eventRepository.DeleteAsync(ev);
            await _notificationService.SendEventDeletedAsync(organizerId, title);
            return new ApiResponse<bool> { Success = true, Message = ApiMessages.EventDeleted, Data = true };
        }

        private Event MapToEntity(CreateEventRequest request)
        {
            var ev = _mapper.Map<Event>(request);
            ev.Slug = request.Title.ToLower().Replace(" ", "-").Replace("'", "");
            return ev;
        }

        private Event ApplyUpdate(UpdateEventRequest request, Event ev)
        {
            _mapper.Map(request, ev);
            return ev;
        }
    }
}
