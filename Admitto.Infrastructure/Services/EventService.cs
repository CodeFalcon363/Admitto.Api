using Admitto.Core.Constants;
using Admitto.Core.Entities;
using Admitto.Core.Models;
using Admitto.Core.Models.Requests.Events;
using Admitto.Core.Models.Responses.Events;
using Admitto.Infrastructure.Data;
using Admitto.Infrastructure.Interfaces.IRepositories;
using Admitto.Infrastructure.Interfaces.IServices;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Text;
using Newtonsoft.Json;
using System.Text.RegularExpressions;

namespace Admitto.Infrastructure.Services
{
    public class EventService : IEventService
    {
        private readonly IEventRepository _eventRepository;
        private readonly IOutboxRepository _outbox;
        private readonly IMapper _mapper;
        private readonly ILogger<EventService> _logger;

        public EventService(
            IEventRepository eventRepository,
            IOutboxRepository outbox,
            IMapper mapper,
            ILogger<EventService> logger)
        {
            _eventRepository = eventRepository;
            _outbox = outbox;
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
                return new ApiResponse<EventResponse>
                {
                    Success = false,
                    Message = ApiMessages.EventNotFound
                };

            return new ApiResponse<EventResponse>
            {
                Success = true,
                Message = ApiMessages.EventFound,
                Data = _mapper.Map<EventResponse>(ev)
            };
        }

        public async Task<ApiResponse<EventResponse>> GetBySlugAsync(string slug)
        {
            var ev = await _eventRepository.GetBySlugAsync(slug);
            if (ev == null)
                return new ApiResponse<EventResponse>
                {
                    Success = false,
                    Message = ApiMessages.EventNotFound
                };

            return new ApiResponse<EventResponse>
            {
                Success = true,
                Message = ApiMessages.EventFound,
                Data = _mapper.Map<EventResponse>(ev)
            };
        }

        public async Task<ApiResponse<EventResponse>> CreateAsync(CreateEventRequest request, Guid organizerId)
        {
            var ev = await MapToEntityAsync(request);
            ev.OrganizerId = organizerId;

            Event created;
            try
            {
                created = await _eventRepository.CreateAsync(ev);
            }
            catch (DbUpdateException ex) when (DbExceptionHelper.IsDuplicateKey(ex))
            {
                // Two concurrent creates with identical titles raced through GenerateUniqueSlugAsync
                // and both tried to insert the same slug. IX_Events_Slug rejected the second one.
                // Regenerate a fresh unique slug and retry once — a second collision is astronomically
                // unlikely and would be caught by the outer exception handler as a 500.
                _logger.LogWarning("Slug collision on concurrent event create for title {Title} — retrying with new slug", request.Title);
                ev.Slug = await GenerateUniqueSlugAsync(request.Title);
                created = await _eventRepository.CreateAsync(ev);
            }

            _logger.LogInformation("Event created: {EventId} - {Title}", created.Id, created.Title);
            await _outbox.EnqueueAsync(
                OutboxEventTypes.EventCreated,
                JsonConvert.SerializeObject(new { Id = created.Id }));

            return new ApiResponse<EventResponse>
            {
                Success = true,
                Message = ApiMessages.EventCreated,
                Data = _mapper.Map<EventResponse>(created)
            };
        }

        public async Task<ApiResponse<EventResponse>> UpdateAsync(string slug, UpdateEventRequest request, Guid? callerUserId = null)
        {
            var ev = await _eventRepository.GetBySlugAsync(slug);
            if (ev == null)
            {
                _logger.LogWarning("Update attempted on non-existent event {Slug}", slug);
                return new ApiResponse<EventResponse>
                {
                    Success = false,
                    Message = ApiMessages.EventNotFound
                };
            }

            if (callerUserId.HasValue && ev.OrganizerId != callerUserId.Value)
            {
                _logger.LogWarning("User {UserId} attempted to update event {Slug} they do not own", callerUserId, slug);
                return new ApiResponse<EventResponse>
                {
                    Success = false,
                    Message = ApiMessages.UnauthorizedAccess
                };
            }

            var updated = await _eventRepository.UpdateAsync(ApplyUpdate(request, ev));
            _logger.LogInformation("Event updated: {EventId}", updated!.Id);
            await _outbox.EnqueueAsync(
                OutboxEventTypes.EventUpdated,
                JsonConvert.SerializeObject(new { Id = ev.Id }));

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
            {
                _logger.LogWarning("Delete attempted on non-existent event {EventId}", id);
                return new ApiResponse<bool>
                {
                    Success = false,
                    Message = ApiMessages.EventNotFound
                };
            }

            var organizerId = ev.OrganizerId;
            var title = ev.Title;
            await _eventRepository.DeleteAsync(ev);
            _logger.LogInformation("Event deleted: {Title} by organizer {OrganizerId}", title, organizerId);
            await _outbox.EnqueueAsync(
                OutboxEventTypes.EventDeleted,
                JsonConvert.SerializeObject(new { OrganizerId = organizerId, EventTitle = title }));

            return new ApiResponse<bool>
            {
                Success = true,
                Message = ApiMessages.EventDeleted,
                Data = true
            };
        }

        private async Task<Event> MapToEntityAsync(CreateEventRequest request)
        {
            var ev = _mapper.Map<Event>(request);
            ev.Slug = await GenerateUniqueSlugAsync(request.Title);
            return ev;
        }

        private async Task<string> GenerateUniqueSlugAsync(string title)
        {
            var baseSlug = Slugify(title);
            var slug = baseSlug;
            var suffix = 1;

            while (await _eventRepository.SlugExistsAsync(slug))
                slug = $"{baseSlug}-{suffix++}";

            return slug;
        }

        /// <summary>
        /// Converts a title to a URL-safe slug:
        ///   1. Unicode normalization (NFD) decomposes accented chars (é → e + combining acute).
        ///   2. Non-spacing combining marks (accents) are stripped.
        ///   3. Every non-alphanumeric character becomes a hyphen.
        ///   4. Consecutive hyphens are collapsed to one.
        ///   5. Leading/trailing hyphens are trimmed.
        /// </summary>
        private static string Slugify(string input)
        {
            var normalized = input.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(normalized.Length);

            foreach (var c in normalized)
            {
                var category = CharUnicodeInfo.GetUnicodeCategory(c);
                if (category == UnicodeCategory.NonSpacingMark) continue;
                sb.Append(char.IsLetterOrDigit(c) ? char.ToLowerInvariant(c) : '-');
            }

            return Regex.Replace(sb.ToString().Trim('-'), "-{2,}", "-");
        }

        private Event ApplyUpdate(UpdateEventRequest request, Event ev)
        {
            _mapper.Map(request, ev);
            return ev;
        }
    }
}
