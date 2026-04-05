using Admitto.Api.Extensions;
using Admitto.Api.Middleware;
using Scalar.AspNetCore;
using Serilog;

// Bootstrap logger captures startup failures before the full Serilog config is loaded.
// Without this, an exception in builder.Build() or ValidateStartupAsync() is silent.
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Replace the default .NET logging pipeline with Serilog.
    // Full configuration (sinks, levels, Seq URL) is read from appsettings.json under "Serilog:".
    builder.Host.UseSerilog((ctx, services, config) =>
        config.ReadFrom.Configuration(ctx.Configuration)
              .ReadFrom.Services(services)
              .Enrich.FromLogContext()
              .Enrich.WithMachineName()
              .Enrich.WithThreadId());

    builder.Services.AddControllers();
    builder.Services.AddOpenApi();
    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
    builder.Services.AddProblemDetails();

    builder.Services.AddDatabase(builder.Configuration);
    builder.Services.AddRedisCache(builder.Configuration);
    builder.Services.AddRepositories();
    builder.Services.AddSettings(builder.Configuration);
    builder.Services.AddStorage(builder.Configuration);
    builder.Services.AddAppServices();
    builder.Services.AddJwtAuthentication(builder.Configuration);
    builder.Services.AddMappings();
    builder.Services.AddAuthRateLimiting();
    builder.Services.AddResponseCompressionDefaults();
    builder.Services.AddOutputCacheDefaults();
    builder.Services.AddAppHealthChecks(builder.Configuration);

    var app = builder.Build();

    await app.ValidateStartupAsync();

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
        app.MapScalarApiReference();
    }

    app.UseResponseCompression();
    app.UseExceptionHandler();
    app.UseHttpsRedirection();
    app.UseSerilogRequestLogging();
    app.UseOutputCache();
    app.UseRateLimiter();
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();
    app.MapAppHealthChecks();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application startup failed");
}
finally
{
    // Ensure all buffered log events are flushed to sinks (Seq, files, etc.)
    // before the process exits — especially important for async sinks.
    await Log.CloseAndFlushAsync();
}
