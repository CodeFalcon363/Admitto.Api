using Admitto.Core.Constants;
using Admitto.Core.Entities;
using Admitto.Core.Models;
using Admitto.Core.Models.Responses.EventMedia;
using Admitto.Infrastructure.Interfaces.IRepositories;
using Admitto.Infrastructure.Interfaces.IServices;
using AutoMapper;
using Microsoft.Extensions.Logging;

namespace Admitto.Infrastructure.Services
{
    public class EventMediaService : IEventMediaService
    {
        private readonly IEventMediaRepository _eventMediaRepository;
        private readonly IEventRepository _eventRepository;
        private readonly IFileService _fileService;
        private readonly IMapper _mapper;
        private readonly ILogger<EventMediaService> _logger;

        public EventMediaService(
            IEventMediaRepository eventMediaRepository,
            IEventRepository eventRepository,
            IFileService fileService,
            IMapper mapper,
            ILogger<EventMediaService> logger)
        {
            _eventMediaRepository = eventMediaRepository;
            _eventRepository = eventRepository;
            _fileService = fileService;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<ApiResponse<EventMediaResponse>> UploadAsync(int eventId, Stream fileStream, string fileName, MediaType type, Guid? callerUserId = null)
        {
            var ev = await _eventRepository.GetByIdAsync(eventId);
            if (ev == null)
                return new ApiResponse<EventMediaResponse>
                {
                    Success = false,
                    Message = ApiMessages.EventNotFound
                };

            if (callerUserId.HasValue && ev.OrganizerId != callerUserId.Value)
                return new ApiResponse<EventMediaResponse>
                {
                    Success = false,
                    Message = ApiMessages.UnauthorizedAccess
                };

            var validationError = await FileValidator.ValidateAsync(fileStream, fileName);
            if (validationError != null)
            {
                _logger.LogWarning("File upload rejected for event {EventId}: {Reason}", eventId, validationError);
                return new ApiResponse<EventMediaResponse>
                {
                    Success = false,
                    Message = validationError
                };
            }

            var url = await _fileService.SaveAsync(fileStream, fileName, "events");
            var created = await _eventMediaRepository.CreateAsync(new EventMedia
            {
                EventId = eventId,
                Url = url,
                Type = type,
                CreatedAt = DateTime.UtcNow
            });

            return new ApiResponse<EventMediaResponse>
            {
                Success = true,
                Message = ApiMessages.MediaUploaded,
                Data = _mapper.Map<EventMediaResponse>(created)
            };
        }

        public async Task<ApiResponse<IEnumerable<EventMediaResponse>>> GetByEventIdAsync(int eventId)
        {
            var media = await _eventMediaRepository.GetAllByEventIdAsync(eventId);

            return new ApiResponse<IEnumerable<EventMediaResponse>>
            {
                Success = true,
                Data = _mapper.Map<IEnumerable<EventMediaResponse>>(media)
            };
        }

        public async Task<ApiResponse<bool>> DeleteAsync(int id, Guid? callerUserId = null)
        {
            var media = await _eventMediaRepository.GetByIdAsync(id);
            if (media == null)
                return new ApiResponse<bool>
                {
                    Success = false,
                    Message = ApiMessages.MediaNotFound
                };

            if (callerUserId.HasValue)
            {
                var ev = await _eventRepository.GetByIdAsync(media.EventId);
                if (ev == null || ev.OrganizerId != callerUserId.Value)
                    return new ApiResponse<bool>
                    {
                        Success = false,
                        Message = ApiMessages.UnauthorizedAccess
                    };
            }

            await _fileService.DeleteAsync(media.Url);
            await _eventMediaRepository.DeleteAsync(media);

            return new ApiResponse<bool>
            {
                Success = true,
                Message = ApiMessages.MediaDeleted,
                Data = true
            };
        }
    }
}
