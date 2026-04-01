using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Admitto.Infrastructure.HealthChecks
{
    public class HttpHealthCheck : IHealthCheck
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _name;
        private readonly string _url;

        public HttpHealthCheck(IHttpClientFactory httpClientFactory, string name, string url)
        {
            _httpClientFactory = httpClientFactory;
            _name = name;
            _url = url;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(5);
                var response = await client.GetAsync(_url, cancellationToken);

                return response.IsSuccessStatusCode
                    ? HealthCheckResult.Healthy($"{_name} is reachable")
                    : HealthCheckResult.Degraded($"{_name} returned {(int)response.StatusCode}");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy($"{_name} is unreachable", ex);
            }
        }
    }
}
