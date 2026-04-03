using Admitto.Core.Models;
using Admitto.Core.Models.Responses.Events;
using Admitto.Core.Settings;
using Admitto.Infrastructure.Interfaces.IServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Web;

namespace Admitto.Infrastructure.Services
{
    public class EventDiscoveryService : IEventDiscoveryService
    {
        private readonly TicketmasterSettings _settings;
        private readonly HttpClient _httpClient;
        private readonly ILogger<EventDiscoveryService> _logger;

        public EventDiscoveryService(
            IOptions<TicketmasterSettings> settings,
            IHttpClientFactory httpClientFactory,
            ILogger<EventDiscoveryService> logger)
        {
            _settings = settings.Value;
            _httpClient = httpClientFactory.CreateClient("ticketmaster");
            _logger = logger;
        }

        public async Task<PagedResponse<ExternalEventResponse>> SearchEventsAsync(string query, int pageNumber, int pageSize)
        {
            try
            {
                // Build URL with query string. ApiKey is in config — not logged in structured logs.
                var qs = HttpUtility.ParseQueryString(string.Empty);
                qs["apikey"]  = _settings.ApiKey;
                qs["keyword"] = query;
                qs["page"]    = (pageNumber - 1).ToString(); // Ticketmaster pages are 0-indexed
                qs["size"]    = pageSize.ToString();
                var url = $"{_settings.BaseUrl}/discovery/v2/events.json?{qs}";

                using var response = await _httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Ticketmaster search failed: {StatusCode}", response.StatusCode);
                    return new PagedResponse<ExternalEventResponse> { Success = false, Message = "External event search failed." };
                }

                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<TicketmasterResponse>(content);
                if (result == null)
                    return new PagedResponse<ExternalEventResponse> { Success = false, Message = "Failed to parse external events." };

                var events = result.Embedded?.Events?.Select(MapToExternalEventResponse) ?? [];

                return new PagedResponse<ExternalEventResponse>
                {
                    Success = true,
                    Data = events,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalRecords = result.Page?.TotalElements ?? 0
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during Ticketmaster search for query: {Query}", query);
                return new PagedResponse<ExternalEventResponse> { Success = false, Message = "External event search failed." };
            }
        }

        private ExternalEventResponse MapToExternalEventResponse(TicketmasterEvent e)
        {
            return new ExternalEventResponse
            {
                Name = e.Name ?? string.Empty,
                Description = e.Info ?? e.PleaseNote ?? string.Empty,
                ExternalBookingUrl = e.Url ?? string.Empty,
                Venue = e.Embedded?.Venues?.FirstOrDefault()?.Name ?? string.Empty,
                ImageUrl = e.Images?.FirstOrDefault(i => i.Ratio == "16_9")?.Url ?? e.Images?.FirstOrDefault()?.Url,
                StartDate = DateTime.TryParse(e.Dates?.Start?.LocalDate, out var start) ? start : default,
                EndDate = DateTime.TryParse(e.Dates?.End?.LocalDate, out var end) ? end : default
            };
        }

        // Internal DTOs for deserializing Ticketmaster API response
        private class TicketmasterResponse
        {
            [JsonPropertyName("_embedded")]
            public TicketmasterEmbedded? Embedded { get; set; }

            [JsonPropertyName("page")]
            public TicketmasterPage? Page { get; set; }
        }

        private class TicketmasterEmbedded
        {
            [JsonPropertyName("events")]
            public List<TicketmasterEvent>? Events { get; set; }
        }

        private class TicketmasterEvent
        {
            [JsonPropertyName("name")]
            public string? Name { get; set; }

            [JsonPropertyName("info")]
            public string? Info { get; set; }

            [JsonPropertyName("pleaseNote")]
            public string? PleaseNote { get; set; }

            [JsonPropertyName("url")]
            public string? Url { get; set; }

            [JsonPropertyName("images")]
            public List<TicketmasterImage>? Images { get; set; }

            [JsonPropertyName("dates")]
            public TicketmasterDates? Dates { get; set; }

            [JsonPropertyName("_embedded")]
            public TicketmasterEventEmbedded? Embedded { get; set; }
        }

        private class TicketmasterImage
        {
            [JsonPropertyName("url")]
            public string? Url { get; set; }

            [JsonPropertyName("ratio")]
            public string? Ratio { get; set; }
        }

        private class TicketmasterDates
        {
            [JsonPropertyName("start")]
            public TicketmasterDate? Start { get; set; }

            [JsonPropertyName("end")]
            public TicketmasterDate? End { get; set; }
        }

        private class TicketmasterDate
        {
            [JsonPropertyName("localDate")]
            public string? LocalDate { get; set; }
        }

        private class TicketmasterEventEmbedded
        {
            [JsonPropertyName("venues")]
            public List<TicketmasterVenue>? Venues { get; set; }
        }

        private class TicketmasterVenue
        {
            [JsonPropertyName("name")]
            public string? Name { get; set; }
        }

        private class TicketmasterPage
        {
            [JsonPropertyName("totalElements")]
            public int TotalElements { get; set; }
        }
    }
}
