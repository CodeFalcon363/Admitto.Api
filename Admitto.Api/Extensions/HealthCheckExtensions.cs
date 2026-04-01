using Admitto.Infrastructure.HealthChecks;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Admitto.Api.Extensions
{
    public static class HealthCheckExtensions
    {
        public static IServiceCollection AddAppHealthChecks(this IServiceCollection services, IConfiguration config)
        {
            var paystackUrl = config.GetValue<string>("PaystackSettings:BaseUrl")!;
            var ticketmasterUrl = config.GetValue<string>("TicketmasterSettings:BaseUrl")!;
            var notificationUrl = config.GetValue<string>("NotificationSettings:BaseUrl")!;

            services.AddHttpClient();
            services.AddHealthChecks()
                .AddCheck<SqlHealthCheck>("sql-server", tags: ["startup", "ready"])
                .AddTypeActivatedCheck<HttpHealthCheck>("paystack", tags: ["ready"],
                    args: ["Paystack", paystackUrl])
                .AddTypeActivatedCheck<HttpHealthCheck>("ticketmaster", tags: ["ready"],
                    args: ["Ticketmaster", ticketmasterUrl])
                .AddTypeActivatedCheck<HttpHealthCheck>("sendgrid", tags: ["ready"],
                    args: ["SendGrid", notificationUrl]);

            return services;
        }

        public static WebApplication MapAppHealthChecks(this WebApplication app)
        {
            app.MapHealthChecks("/health");
            app.MapHealthChecks("/health/ready", new HealthCheckOptions
            {
                Predicate = check => check.Tags.Contains("ready")
            });
            return app;
        }

        public static async Task ValidateStartupAsync(this WebApplication app)
        {
            var logger = app.Services.GetRequiredService<ILogger<Program>>();
            var healthCheckService = app.Services.GetRequiredService<HealthCheckService>();

            logger.LogInformation("Running startup health checks...");

            var result = await healthCheckService.CheckHealthAsync(
                check => check.Tags.Contains("startup"));

            foreach (var (name, entry) in result.Entries)
            {
                if (entry.Status == HealthStatus.Healthy)
                    logger.LogInformation("  {Name}: Healthy — {Description}", name, entry.Description);
                else
                    logger.LogCritical("  {Name}: {Status} — {Description}", name, entry.Status, entry.Description ?? entry.Exception?.Message);
            }

            if (result.Status != HealthStatus.Healthy)
                throw new Exception("Critical startup health check failed. See logs above.");
        }
    }
}
