using Admitto.Core.Data;
using Admitto.Core.Settings;
using Admitto.Infrastructure.Interfaces.IRepositories;
using Admitto.Infrastructure.Interfaces.IServices;
using Admitto.Infrastructure.Mappings;
using Admitto.Infrastructure.Repositories;
using Admitto.Infrastructure.Services;
using Amazon.S3;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;
using System.Text;

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
            // Supports both standalone and Sentinel.
            // Sentinel connection string format: "sentinel1:26379,sentinel2:26379,sentinel3:26379,serviceName=mymaster"
            services.AddSingleton<IConnectionMultiplexer>(_ =>
                ConnectionMultiplexer.Connect(config.GetConnectionString("Redis")!));
            services.AddScoped<ICacheService, CacheService>();
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
            services.AddScoped<IFileService, S3FileService>();
            services.AddScoped<INotificationResolver, NotificationResolver>();
            services.AddScoped<INotificationRuleService, NotificationRuleService>();
            services.AddScoped<INotificationPreferenceService, NotificationPreferenceService>();
            services.AddHostedService<EventReminderBackgroundService>();
            return services;
        }

        public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration config)
        {
            var jwtSettings = config.GetSection("JwtSettings").Get<JwtSettings>()!;
            services.Configure<JwtSettings>(config.GetSection("JwtSettings"));

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
            return services;
        }

        public static IServiceCollection AddS3(this IServiceCollection services, IConfiguration config)
        {
            services.AddSingleton<IAmazonS3>(sp =>
            {
                var settings = sp.GetRequiredService<IOptions<S3Settings>>().Value;
                return new AmazonS3Client(
                    settings.AccessKey,
                    settings.SecretKey,
                    Amazon.RegionEndpoint.GetBySystemName(settings.Region));
            });
            return services;
        }

        public static IServiceCollection AddMappings(this IServiceCollection services)
        {
            services.AddAutoMapper(typeof(MappingProfile));
            return services;
        }
    }
}
