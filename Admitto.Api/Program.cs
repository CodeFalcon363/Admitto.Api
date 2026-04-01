using Admitto.Api.Extensions;
using Admitto.Api.Middleware;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

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
builder.Services.AddAppHealthChecks(builder.Configuration);

var app = builder.Build();

await app.ValidateStartupAsync();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseExceptionHandler();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapAppHealthChecks();

app.Run();
