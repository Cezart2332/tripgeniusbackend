using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Scalar.AspNetCore;
using TripGeniusBackend.Application.Interfaces;
using TripGeniusBackend.Application.Interfaces.Services;
using TripGeniusBackend.Application.UseCases;
using TripGeniusBackend.Infrastructure.Persistence;
using TripGeniusBackend.Infrastructure.Persistence.Hubs;
using TripGeniusBackend.Infrastructure.Persistence.Repositories;
using TripGeniusBackend.Infrastructure.Persistence.Services;
using TripGeniusBackend.Infrastructure.Services;
using TripGeniusBackend.Application.Interfaces.Queries;
using TripGeniusBackend.Application.Interfaces.Repositories;
using TripGeniusBackend.Application.Interfaces.UseCases;
using TripGeniusBackend.Infrastructure.Persistence.Queries;
using Resend;
using TripGeniusBackend.API.Middleware;
using TripGeniusBackend.Application.Settings;
using TripGeniusBackend.Infrastructure.Persistence.Hubs;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using Npgsql;
using Pgvector.Npgsql;
using QuestPDF.Infrastructure;
using WebPush;

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

DotNetEnv.Env.TraversePath().Load();

QuestPDF.Settings.License = LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration
    .AddJsonFile("appsettings.json")
    .AddEnvironmentVariables();

builder.Services.AddRateLimiter(options =>
{
    static string PartitionKey(HttpContext ctx) =>
        ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";

    // Politica globală — 30 requests / minut per IP
    options.AddPolicy("global", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(PartitionKey(httpContext), _ =>
            new FixedWindowRateLimiterOptions
            {
                PermitLimit = 30,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0,
            }));

    // Politica pentru auth — mai strictă (5 requests / minut per IP)
    options.AddPolicy("auth", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter("auth-" + PartitionKey(httpContext), _ =>
            new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0,
            }));

    // Ce returnezi când e blocat
    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.StatusCode = 429;
        await context.HttpContext.Response.WriteAsync(
            "Too many requests. Please try again later.", token);
    };
});


builder.Services.AddSignalR();
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });


var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
{
    connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection") 
                       ?? "Host=localhost;Port=5432;Database=tripgenius;Username=postgres;Password=password";
}
var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
dataSourceBuilder.ConnectionStringBuilder.SearchPath = "public"; // setare explicită, nu din connection string
dataSourceBuilder.UseVector();
var dataSource = dataSourceBuilder.Build();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(dataSource, o =>{
        o.UseVector();
        o.MigrationsHistoryTable("__EFMigrationsHistory", "public");
        })
        .ConfigureWarnings(w => 
            w.Ignore(RelationalEventId.PendingModelChangesWarning)));
    
builder.Services.AddHttpContextAccessor();

builder.Services.Configure<GoogleSettings>(
    builder.Configuration.GetSection("Google")
);
builder.Services.Configure<OpenRouterSettings>(
    builder.Configuration.GetSection("OpenRouter")
);
builder.Services.Configure<OpenTripMapSettings>(
    builder.Configuration.GetSection("OpenTripMap")
);
builder.Services.Configure<VapidSettings>(
    builder.Configuration.GetSection("Vapid")
);
builder.Services.Configure<ModerationSettings>(
    builder.Configuration.GetSection("Moderation")
);
builder.Services.Configure<CorsSettings>(
    builder.Configuration.GetSection("Cors")
);
//Application
builder.Services.AddMemoryCache();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ITripService, TripService>();
builder.Services.AddScoped<IOffroadTripService, OffroadTripService>();
builder.Services.AddScoped<IBugService, BugService>();
builder.Services.AddScoped<IAiChatService, AiChatService>();
builder.Services.AddHttpClient<ResendClient>();
builder.Services.AddHttpClient<IAiService,AiService>(client =>
{
    client.Timeout = TimeSpan.FromMinutes(15);
});
builder.Services.AddHttpClient<IAgentRunner, AgentRunner>(client =>
{
    client.Timeout = TimeSpan.FromMinutes(5);
});
builder.Services.AddHttpClient<IEmbeddingService, EmbeddingService>();
builder.Services.AddHttpClient<GeocodingService>(client =>
{
    client.DefaultRequestHeaders.Add("User-Agent", "TripGenius/1.0");
    client.DefaultRequestHeaders.Add("Accept-Language", "ro");
});
builder.Services.AddHttpClient<IContentModerationService, ContentModerationService>((sp, client) =>
{
    var settings = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<ModerationSettings>>().Value;
    if (!string.IsNullOrWhiteSpace(settings.BaseUrl))
        client.BaseAddress = new Uri(settings.BaseUrl.TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromSeconds(ModerationHttpTimeouts.ClientSeconds(settings.TimeoutSeconds));
});
builder.Services.AddSingleton<IBackgroundModerationService, BackgroundModerationService>();
builder.Services.AddHttpClient(nameof(ModerationStartupLogger));
builder.Services.AddHostedService<ModerationStartupLogger>();
builder.Services.AddHttpClient<ILinkValidationService, LinkValidationService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(10);
});
builder.Services.AddScoped<ITripChatAiService, TripChatAiService>();
builder.Services.Configure<ResendClientOptions>( o =>
{
    o.ApiToken = builder.Configuration["Email:ResendApiKey"]!;
} );
builder.Services.AddTransient<IResend, ResendClient>();
//Infrastructure
builder.Services.AddSingleton<WebPushClient>();
builder.Services.AddScoped<IUserRepository,UserRepository>();

builder.Services.AddScoped<IAiMemoryRepository, AiMemoryRepository>();
builder.Services.AddScoped<IAiChatRepository,AiChatRepository>();
builder.Services.AddScoped<IBugRepository,BugRepository>();
builder.Services.AddScoped<ITripRepository,TripRepository>();
builder.Services.AddScoped<IOffroadTripRepository, OffroadTripRepository>();
builder.Services.AddScoped<IOffroadMessageRepository, OffroadMessageRepository>();
builder.Services.AddScoped<IRefreshTokenRepository,RefreshTokenRepository>();
builder.Services.AddScoped<IMessageRepository, MessageRepository>();
builder.Services.AddScoped<IUserQueryService, UserQueryService>();
builder.Services.AddScoped<ITripQueryService, TripQueryService>();
builder.Services.AddScoped<IOffroadTripQueryService, OffroadTripQueryService>();
builder.Services.AddScoped<IOffroadMessageQueryService, OffroadMessageQueryService>();
builder.Services.AddScoped<IGpxService, GpxService>();
builder.Services.AddScoped<IAiChatQueryService, AiChatQueryService>();
builder.Services.AddScoped<IBugQueryService, BugQueryService>();
builder.Services.AddScoped<IMessageQueryService, MessageQueryService>();
builder.Services.AddScoped<IRefreshTokenQueryService, RefreshTokenQueryService>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<ITokenHasher, TokenHasher>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddSingleton<INotificationService, NotificationService>();
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<IPdfService, PdfService>();
builder.Services.AddScoped<IFileUploader, FileUploader>();
builder.Services.AddSingleton<IUserIdProvider, UserIdProvider>();

builder.Services.AddAuthentication(options => 
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme; 
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        }
    )
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                System.Text.Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
        };
        
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];

                var path = context.HttpContext.Request.Path;


                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                    context.Token = accessToken;

                return Task.CompletedTask;
            }
        };
    });



builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("PostgreSQL", tags: new[] { "ready" })
    .AddCheck<EmailServiceHealthCheck>("EmailService", tags: new[] { "live" })
    .AddCheck<AiServiceHealthCheck>("AiService", tags: new[] { "live" });
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, ct) =>
   
    {
        document.Info = new OpenApiInfo
        {
            Title = "TripGenius API",
            Version = "v1"
        };
        
        document.Components ??= new();
        document.Components.SecuritySchemes = new Dictionary<string, IOpenApiSecurityScheme>
        {
            ["Bearer"] = new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT"
            }
        };
        return Task.CompletedTask;
    });
});

var corsOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>()?
    .Where(static origin => !string.IsNullOrWhiteSpace(origin))
    .Select(static origin => origin.Trim().TrimEnd('/'))
    .Distinct(StringComparer.OrdinalIgnoreCase)
    .ToArray() ?? [];

if (corsOrigins.Length == 0)
{
    if (builder.Environment.IsDevelopment())
    {
        corsOrigins =
        [
            "http://localhost:5173",
            "http://localhost:5174",
            "http://localhost:4173",
        ];
    }
    else
    {
        throw new InvalidOperationException(
            "Cors:AllowedOrigins must be configured in non-development environments.");
    }
}

builder.Services.AddCors(options =>
{
    options.AddPolicy("frontend", policy =>
    {
        policy.WithOrigins(corsOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});


var app = builder.Build();

app.Logger.LogInformation("CORS allowed origins: {Origins}", string.Join(", ", corsOrigins));

app.MapOpenApi();
app.MapScalarApiReference(); 


var forwardedOptions = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
};
// The app is only reachable through the reverse proxy (nginx), which runs on a
// non-loopback container IP. Clear the default trusted list so X-Forwarded-For is
// honored and RemoteIpAddress reflects the real client (needed for per-IP rate limiting).
// If the backend is ever exposed directly, restrict this to the proxy's network instead.
forwardedOptions.KnownNetworks.Clear();
forwardedOptions.KnownProxies.Clear();
app.UseForwardedHeaders(forwardedOptions);

app.UseRouting();

app.UseCors("frontend");
//app.UseHsts();
//app.UseHttpsRedirection();

var uploadsPath = Environment.GetEnvironmentVariable("UPLOADS_PATH") 
    ?? Path.Combine(builder.Environment.ContentRootPath, "wwwroot");

// Creează folderul dacă nu există
Directory.CreateDirectory(uploadsPath);

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadsPath),
    RequestPath = ""
});
// TLS is terminated at the reverse proxy (nginx), which is responsible for the
// HTTP->HTTPS redirect. We still emit HSTS in non-dev so browsers pin HTTPS.
var enableHsts = !app.Environment.IsDevelopment();
app.Use(async (ctx, next) =>
{
    ctx.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    ctx.Response.Headers.Append("X-Frame-Options", "DENY");
    ctx.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
    if (enableHsts)
        ctx.Response.Headers.Append("Strict-Transport-Security", "max-age=63072000; includeSubDomains");
    await next();
});
app.UseMiddleware<ExceptionMiddleware>(); 
app.UseMiddleware<LoggingMiddleware>();
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

// De-duplicates replayed mutations carrying an Idempotency-Key (e.g. from the
// frontend offline queue). Runs after auth so keys are scoped to the user.
app.UseMiddleware<IdempotencyMiddleware>();

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("live") || check.Tags.Contains("ready")
});

// Alias backward compatibility
app.MapHealthChecks("/health", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});
app.MapControllers()
    .RequireRateLimiting("global")
    .RequireCors("frontend");
app.MapHub<TripChatHub>("/hubs/trip-chat").RequireCors("frontend");
app.MapHub<OffroadTripChatHub>("/hubs/offroad-trip-chat").RequireCors("frontend");
app.MapHub<AiChatHub>("/hubs/ai-chat").RequireCors("frontend");

using var scope = app.Services.CreateScope();
var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
if (db.Database.IsRelational())
{
    db.Database.Migrate();
}
await db.SaveChangesAsync();
app.Run();

public class DatabaseHealthCheck : IHealthCheck
{
    private readonly AppDbContext _context;
    public DatabaseHealthCheck(AppDbContext context) => _context = context;

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            if (await _context.Database.CanConnectAsync(cancellationToken))
            {
                return HealthCheckResult.Healthy("PostgreSQL database is connected.");
            }
            return HealthCheckResult.Unhealthy("Cannot connect to PostgreSQL database.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("PostgreSQL database connection failed.", ex);
        }
    }
}

public class EmailServiceHealthCheck : IHealthCheck
{
    private readonly IConfiguration _config;
    public EmailServiceHealthCheck(IConfiguration config) => _config = config;

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var apiKey = _config["Email:ResendApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("Resend API key is not configured."));
        }
        return Task.FromResult(HealthCheckResult.Healthy("Resend API is configured."));
    }
}

public class AiServiceHealthCheck : IHealthCheck
{
    private readonly IConfiguration _config;
    public AiServiceHealthCheck(IConfiguration config) => _config = config;

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var apiKey = _config["OpenRouter:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("OpenRouter API key is not configured."));
        }
        return Task.FromResult(HealthCheckResult.Healthy("OpenRouter API is configured."));
    }
}