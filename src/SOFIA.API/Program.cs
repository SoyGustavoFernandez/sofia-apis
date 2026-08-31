using SOFIA.API.Services;
using SOFIA.Application.Common.Interfaces;
using SOFIA.API.Infrastructure;
using SOFIA.Application;
using SOFIA.Infrastructure;
using SOFIA.Domain;
using SOFIA.SharedKernel;
using Serilog;
using Serilog.Formatting.Compact;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

using System.IdentityModel.Tokens.Jwt;

JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

var builder = WebApplication.CreateBuilder(args);

// --- Structured Logging with Serilog ---
builder.Host.UseSerilog((ctx, cfg) => cfg
    .ReadFrom.Configuration(ctx.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .WriteTo.Console(new CompactJsonFormatter())
    .WriteTo.File(
        new CompactJsonFormatter(),
        path: "logs/sofia-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        fileSizeLimitBytes: 50 * 1024 * 1024));

// --- HSTS ---
_ = builder.Services.AddHsts(options =>
{
    options.MaxAge = TimeSpan.FromDays(365);
    options.IncludeSubDomains = true;
});

// --- CORS Configuration ---
var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>();
_ = builder.Services.AddCors(options =>
{
    options.AddPolicy("SofiaCorsPolicy", policy =>
    {
        if (builder.Environment.IsDevelopment())
        {
            _ = policy.WithOrigins("http://localhost:4200", "https://localhost:4200")
                      .AllowAnyMethod()
                      .AllowAnyHeader()
                      .AllowCredentials();
        }
        else
        {
            if (allowedOrigins == null || allowedOrigins.Length == 0)
            {
                throw new InvalidOperationException("CRITICAL: 'AllowedOrigins' is not configured for the production environment.");
            }

            _ = policy.WithOrigins(allowedOrigins)
                      .AllowAnyMethod()
                      .AllowAnyHeader()
                      .AllowCredentials();
        }
    });
});

// --- Container Services ---
_ = builder.Services.AddControllers();
_ = builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
});

// Modern Error Handling
_ = builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
_ = builder.Services.AddProblemDetails();

// --- Rate Limiting ---
_ = builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // Catch-all: 200 req/min per IP for any endpoint without a named policy
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(ctx =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 200,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 10
            }));

    // Auth: 10 attempts/min per IP — brute-force protection for login/register
    _ = options.AddFixedWindowLimiter("auth", o =>
    {
        o.PermitLimit = 10;
        o.Window = TimeSpan.FromMinutes(1);
        o.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        o.QueueLimit = 2;
    });

    // AI endpoints (prescription digitization): strict limit due to processing cost
    _ = options.AddFixedWindowLimiter("ai-endpoints", o =>
    {
        o.PermitLimit = 10;
        o.Window = TimeSpan.FromMinutes(1);
        o.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        o.QueueLimit = 2;
    });
});

// Infrastructure Services
_ = builder.Services.AddHttpContextAccessor();

// Clean Architecture Layers Registration
_ = builder.Services.AddScoped<ICurrentUser, CurrentUser>();
_ = builder.Services.AddSharedKernel();
_ = builder.Services.AddDomain();
_ = builder.Services.AddApplication();
_ = builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

// --- HTTP Request Pipeline ---
_ = app.UseExceptionHandler();
_ = app.UseSerilogRequestLogging();
_ = app.UseMiddleware<SecurityHeadersMiddleware>();

if (!app.Environment.IsDevelopment())
{
    _ = app.UseHsts();
}

_ = app.UseHttpsRedirection();

_ = app.UseCors("SofiaCorsPolicy");

_ = app.UseAuthentication();
_ = app.UseAuthorization();
_ = app.UseRateLimiter();

_ = app.MapControllers();

// Health Check Endpoint
var version = (System.Reflection.AssemblyInformationalVersionAttribute?)
    Attribute.GetCustomAttribute(
        typeof(Program).Assembly,
        typeof(System.Reflection.AssemblyInformationalVersionAttribute))
    is { } attr ? attr.InformationalVersion : "unknown";

_ = app.MapGet("/health", () => Results.Ok(new
{
    Status = "Healthy",
    Version = version,
    Timestamp = DateTimeOffset.UtcNow,
    Environment = app.Environment.EnvironmentName
}))
.WithName("GetHealth");

await app.RunAsync();
