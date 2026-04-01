using Admitto.Core.Data;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Admitto.Infrastructure.HealthChecks
{
    public class SqlHealthCheck : IHealthCheck
    {
        private readonly AdmittoDbContext _context;

        public SqlHealthCheck(AdmittoDbContext context)
        {
            _context = context;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                await _context.Database.CanConnectAsync(cancellationToken);
                return HealthCheckResult.Healthy("SQL Server is reachable");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy("SQL Server is unreachable", ex);
            }
        }
    }
}
