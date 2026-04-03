using Admitto.Core.Data;
using Admitto.Core.Settings;
using Admitto.Infrastructure.Http;
using Admitto.Infrastructure.Interfaces.IRepositories;
using Admitto.Infrastructure.Interfaces.IServices;
using Admitto.Infrastructure.Mappings;
using Admitto.Infrastructure.Repositories;
using Admitto.Infrastructure.Services;
using Amazon.S3;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;
using System.Text;
using System.Threading.RateLimiting;

namespace Admitto.Api.Extensions
{
    public static class ServiceExtensions
    {
        public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration config)
        {
            services.AddDbContext<AdmittoDbContext>(options =>
                options.UseSqlServer(config.GetConnectionString("DefaultConnection")));
            return services;
        }

        public static IServiceCollection AddRedisCache(this IServiceCollection services, IConfiguration config)
        {
            var redisMode = config.GetValue<string>("RedisSettings:Mode") ?? "Standalone";

            var abortOnConnectFail = config.GetValue<bool>("RedisSettings:AbortOnConnectFail", defaultValue: true);

            services.AddSingleton<IConnectionMultiplexer>(_ =>
            {
                if (redisMode.Equals("Sentinel", StringComparison.OrdinalIgnoreCase))
                {
                    var sentinelName = config.GetValue<string>("RedisSettings:SentinelServiceName") ?? "mymaster";
                    var options = ConfigurationOptions.Parse(config.GetConnectionString("Redis")!);
                    options.ServiceName = sentinelName;
                    options.AbortOnConnectFail = abortOnConnectFail;
                    return ConnectionMultiplexer.Connect(options);
                }

                var standaloneOptions = ConfigurationOptions.Parse(config.GetConnectionString("Redis")!);
                standaloneOptions.AbortOnConnectFail = abortOnConnectFail;
                return ConnectionMultiplexer.Connect(standaloneOptions);
            });

            // Singleton: CacheService wraps IConnectionMultiplexer (already Singleton) and
            // holds no request-scoped state. Scoped was creating a new wrapper per request
            // for no reason.
            services.AddSingleton<ICacheService, CacheService>();
            return services;
        }

        public static IServiceCollection AddStorage(this IServiceCollection services, IConfiguration config)
        {
            var provider = config.GetValue<string>("StorageSettings:Provider") ?? "S3";

            if (provider.Equals("Local", StringComparison.OrdinalIgnoreCase))
            {
                services.AddScoped<IFileService, FileService>();
            }
            else
            {
                services.AddSingleton<IAmazonS3>(sp =>
                {
                    var settings = sp.GetRequiredService<IOptions<S3Settings>>().Value;
                    return new AmazonS3Client(
                        settings.AccessKey,
                        settings.SecretKey,
                        Amazon.RegionEndpoint.GetBySystemName(settings.Region));
                });
                services.AddScoped<IFileService, S3FileService>();
            }

            return services;
        }

        public static IServiceCollection AddRepositories(this IServiceCollection services)
        {
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IEventRepository, EventRepository>();
            services.AddScoped<ITicketTypeRepository, TicketTypeRepository>();
            services.AddScoped<IBookingRepository, BookingRepository>();
            services.AddScoped<IPaymentRepository, PaymentRepository>();
            services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
            services.AddScoped<IEventMediaRepository, EventMediaRepository>();
            services.AddScoped<IPasswordResetTokenRepository, PasswordResetTokenRepository>();
            services.AddScoped<INotificationRuleRepository, NotificationRuleRepository>();
            services.AddScoped<IUserNotificationPreferenceRepository, UserNotificationPreferenceRepository>();
            services.AddScoped<IEventReminderOverrideRepository, EventReminderOverrideRepository>();
            services.AddScoped<IOrganizerReminderSettingRepository, OrganizerReminderSettingRepository>();
            return services;
        }

        public static IServiceCollection AddAppServices(this IServiceCollection services)
        {
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IEventService, EventService>();
            services.AddScoped<ITicketTypeService, TicketTypeService>();
            services.AddScoped<IBookingService, BookingService>();
            services.AddScoped<IPaymentService, PaymentService>();
            services.AddScoped<INotificationService, NotificationService>();
            services.AddScoped<IEventDiscoveryService, EventDiscoveryService>();
            services.AddScoped<IEventMediaService, EventMediaService>();
            services.AddScoped<INotificationResolver, NotificationResolver>();
            services.AddScoped<INotificationRuleService, NotificationRuleService>();
            services.AddScoped<INotificationPreferenceService, NotificationPreferenceService>();
            services.AddHostedService<EventReminderBackgroundService>();

            // Named HttpClients — shared, pooled connections managed by IHttpClientFactory.
            // Prevents socket exhaustion caused by instantiating RestClient (or HttpClient)
            // per-request, which leaves sockets in TIME_WAIT under any meaningful load.
            services.AddHttpClient("notification");

            // TicketmasterApiKeyHandler injects the API key as a header so it never
            // appears in the URL (and therefore never in access logs or proxy logs).
            services.AddTransient<TicketmasterApiKeyHandler>();
            services.AddHttpClient("ticketmaster")
                .AddHttpMessageHandler<TicketmasterApiKeyHandler>();

            return services;
        }

        public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration config)
        {
            var jwtSettings = config.GetSection("JwtSettings").Get<JwtSettings>()!;
            services.Configure<JwtSettings>(config.GetSection("JwtSettings"));
            services.AddSingleton<IValidateOptions<JwtSettings>, JwtSettingsValidator>();

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSettings.Issuer,
                    ValidAudience = jwtSettings.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
                    ClockSkew = TimeSpan.Zero
                };
            });

            return services;
        }

        public static IServiceCollection AddSettings(this IServiceCollection services, IConfiguration config)
        {
            services.Configure<PaystackSettings>(config.GetSection("PaystackSettings"));
            services.Configure<TicketmasterSettings>(config.GetSection("TicketmasterSettings"));
            services.Configure<NotificationSettings>(config.GetSection("NotificationSettings"));
            services.Configure<FileSettings>(config.GetSection("FileSettings"));
            services.Configure<S3Settings>(config.GetSection("S3Settings"));
            services.Configure<StorageSettings>(config.GetSection("StorageSettings"));
            services.Configure<RedisSettings>(config.GetSection("RedisSettings"));
            return services;
        }

        public static IServiceCollection AddMappings(this IServiceCollection services)
        {
            services.AddAutoMapper(cfg => cfg.AddProfile<MappingProfile>());
            return services;
        }

        /// <summary>
        /// Rate-limits auth endpoints that run BCrypt or send emails.
        /// 10 requests per IP per minute is generous for legitimate use and blocks brute-force.
        /// </summary>
        public static IServiceCollection AddAuthRateLimiting(this IServiceCollection services)
        {
            services.AddRateLimiter(options =>
            {
                options.AddPolicy("auth", httpContext =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 10,
                            Window = TimeSpan.FromMinutes(1),
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                            QueueLimit = 0
                        }));

                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            });

            return services;
        }
    }
}
