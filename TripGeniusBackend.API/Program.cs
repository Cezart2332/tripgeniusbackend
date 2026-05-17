using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
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
using TripGeniusBackend.Application.UseCases;
using TripGeniusBackend.Infrastructure.Persistence;
using TripGeniusBackend.Infrastructure.Persistence.Repositories;
using TripGeniusBackend.Infrastructure.Persistence.Services;
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
    // Politica globală — 30 requests / minut per IP
    options.AddFixedWindowLimiter("global", opt =>
    {
        opt.PermitLimit = 30;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 0;
    });

    // Politica pentru auth — mai strictă (5 requests / minut)
    options.AddFixedWindowLimiter("auth", opt =>
    {
        opt.PermitLimit = 5;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueLimit = 0;
    });

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


builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"), o => o.UseVector())
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
//Application
builder.Services.AddMemoryCache();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ITripService, TripService>();
builder.Services.AddScoped<IBugService, BugService>();
builder.Services.AddScoped<IAiChatService, AiChatService>();
builder.Services.AddHttpClient<ResendClient>();
builder.Services.AddHttpClient<IAiService,AiService>(client =>
{
    client.Timeout = TimeSpan.FromMinutes(10); 
});
builder.Services.AddHttpClient<IEmbeddingService, EmbeddingService>();
builder.Services.AddHttpClient<GeocodingService>(client =>
{
    client.DefaultRequestHeaders.Add("User-Agent", "TripGenius/1.0");
    client.DefaultRequestHeaders.Add("Accept-Language", "ro");
});
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
builder.Services.AddScoped<IRefreshTokenRepository,RefreshTokenRepository>();
builder.Services.AddScoped<IMessageRepository, MessageRepository>();
builder.Services.AddScoped<IUserQueryService, UserQueryService>();
builder.Services.AddScoped<ITripQueryService, TripQueryService>();
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

builder.Services.AddCors(options =>
{
    options.AddPolicy("frontend", policy =>
    {
        policy.WithOrigins("https://tripgenius.online","http://localhost:5173","http://localhost:5174", "http://localhost:4173")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});
Console.WriteLine(builder.Configuration.GetConnectionString("DefaultConnection"));


var app = builder.Build();



app.MapOpenApi();
app.MapScalarApiReference(); 


app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});
app.UseHsts();
app.UseHttpsRedirection();

var uploadsPath = Environment.GetEnvironmentVariable("UPLOADS_PATH") 
    ?? Path.Combine(builder.Environment.ContentRootPath, "wwwroot");

// Creează folderul dacă nu există
Directory.CreateDirectory(uploadsPath);

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadsPath),
    RequestPath = ""
});
app.Use(async (ctx, next) =>
{
    ctx.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    ctx.Response.Headers.Append("X-Frame-Options", "DENY");
    ctx.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
    await next();
});
app.UseMiddleware<ExceptionMiddleware>(); 
app.UseMiddleware<LoggingMiddleware>();
app.UseCors("frontend");
app.UseRateLimiter();


app.UseAuthentication(); 
app.UseAuthorization();


app.MapControllers().RequireRateLimiting("global");
app.MapHub<TripChatHub>("/hubs/trip-chat");
app.MapHub<AiChatHub>("/hubs/ai-chat");

using var scope = app.Services.CreateScope();
var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
db.Database.Migrate();
await db.SaveChangesAsync();
app.Run();