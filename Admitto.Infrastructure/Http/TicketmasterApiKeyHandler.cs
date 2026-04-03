using Admitto.Core.Settings;
using Microsoft.Extensions.Options;

namespace Admitto.Infrastructure.Http
{
    /// <summary>
    /// Injects the Ticketmaster API key as an X-API-Key request header.
    /// Keeping the key out of the URL prevents it from appearing in web server access logs,
    /// reverse-proxy logs, and browser history.
    /// </summary>
    public class TicketmasterApiKeyHandler : DelegatingHandler
    {
        private readonly TicketmasterSettings _settings;

        public TicketmasterApiKeyHandler(IOptions<TicketmasterSettings> settings)
        {
            _settings = settings.Value;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            request.Headers.TryAddWithoutValidation("X-Api-Key", _settings.ApiKey);
            return base.SendAsync(request, cancellationToken);
        }
    }
}
