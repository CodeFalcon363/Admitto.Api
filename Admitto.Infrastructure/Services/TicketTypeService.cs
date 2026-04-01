using Admitto.Core.Constants;
using Admitto.Core.Entities;
using Admitto.Core.Models;
using Admitto.Core.Models.Requests.TicketTypes;
using Admitto.Core.Models.Responses.TicketTypes;
using Admitto.Infrastructure.Interfaces.IRepositories;
using Admitto.Infrastructure.Interfaces.IServices;
using AutoMapper;
using Microsoft.Extensions.Logging;

namespace Admitto.Infrastructure.Services
{
    public class TicketTypeService : ITicketTypeService
    {
        private readonly ITicketTypeRepository _ticketTypeRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<TicketTypeService> _logger;

        public TicketTypeService(ITicketTypeRepository ticketTypeRepository, IMapper mapper, ILogger<TicketTypeService> logger)
        {
            _ticketTypeRepository = ticketTypeRepository;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<PagedResponse<TicketTypeResponse>> GetAllByEventSlugAsync(string slug, int pageNumber, int pageSize)
        {
            var (data, totalRecords) = await _ticketTypeRepository.GetAllByEventSlugAsync(slug, pageNumber, pageSize);

            return new PagedResponse<TicketTypeResponse>
            {
                Success = true,
                Data = _mapper.Map<IEnumerable<TicketTypeResponse>>(data),
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalRecords = totalRecords
            };
        }

        public async Task<ApiResponse<TicketTypeResponse>> GetByIdAsync(int id)
        {
            var ticketType = await _ticketTypeRepository.GetByIdAsync(id);
            if (ticketType == null)
                return new ApiResponse<TicketTypeResponse>
                {
                    Success = false,
                    Message = ApiMessages.TicketTypeNotFound
                };

            return new ApiResponse<TicketTypeResponse>
            {
                Success = true,
                Data = _mapper.Map<TicketTypeResponse>(ticketType)
            };
        }

        public async Task<ApiResponse<TicketTypeResponse>> CreateAsync(CreateTicketTypeRequest request)
        {
            var created = await _ticketTypeRepository.CreateAsync(_mapper.Map<TicketType>(request));

            return new ApiResponse<TicketTypeResponse>
            {
                Success = true,
                Message = ApiMessages.TicketTypeCreated,
                Data = _mapper.Map<TicketTypeResponse>(created)
            };
        }

        public async Task<ApiResponse<TicketTypeResponse>> UpdateAsync(int id, UpdateTicketTypeRequest request)
        {
            var ticketType = await _ticketTypeRepository.GetByIdAsync(id);
            if (ticketType == null)
                return new ApiResponse<TicketTypeResponse>
                {
                    Success = false,
                    Message = ApiMessages.TicketTypeNotFound
                };

            var updated = await _ticketTypeRepository.UpdateAsync(ApplyUpdate(request, ticketType));

            return new ApiResponse<TicketTypeResponse>
            {
                Success = true,
                Message = ApiMessages.TicketTypeUpdated,
                Data = _mapper.Map<TicketTypeResponse>(updated)
            };
        }

        public async Task<ApiResponse<bool>> DeleteAsync(int id)
        {
            var ticketType = await _ticketTypeRepository.GetByIdAsync(id);
            if (ticketType == null)
                return new ApiResponse<bool>
                {
                    Success = false,
                    Message = ApiMessages.TicketTypeNotFound
                };

            await _ticketTypeRepository.DeleteAsync(ticketType);

            return new ApiResponse<bool>
            {
                Success = true,
                Message = ApiMessages.TicketTypeDeleted,
                Data = true
            };
        }

        private TicketType ApplyUpdate(UpdateTicketTypeRequest request, TicketType ticketType)
        {
            _mapper.Map(request, ticketType);
            return ticketType;
        }
    }
}
