using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Identity;
using Asp.Versioning;
using Microsoft.OpenApi.Models;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using SpeedClaim.Api.Context;
using SpeedClaim.Api.Configuration;
using SpeedClaim.Api.Hubs;
using SpeedClaim.Api.Swagger;
using SpeedClaim.Api.Exceptions;
using SpeedClaim.Api.Interfaces;
using SpeedClaim.Api.Repositories;
using SpeedClaim.Api.Services;
using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using Stripe;

var builder = WebApplication.CreateBuilder(args);

var keyVaultUri = builder.Configuration["KeyVault:Uri"];
if (!string.IsNullOrWhiteSpace(keyVaultUri))
{
    builder.Configuration.AddAzureKeyVault(new Uri(keyVaultUri), new DefaultAzureCredential());
}

var blobLogOptions = builder.Configuration
    .GetSection(BlobLogStorageOptions.SectionName)
    .Get<BlobLogStorageOptions>() ?? new BlobLogStorageOptions();
var blobLogConnectionString = blobLogOptions.ConnectionString
    ?? builder.Configuration["AzureBlob:ConnectionString"];
BlobLogBatchingSink? blobLogSink = null;
if (blobLogOptions.Enabled && !string.IsNullOrWhiteSpace(blobLogConnectionString))
{
    blobLogSink = new BlobLogBatchingSink(blobLogOptions, blobLogConnectionString);
    builder.Services.AddSingleton(blobLogSink);
    builder.Services.AddHostedService<BlobLogBackgroundService>();
}

// 1. Configure Serilog
var loggerConfiguration = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console();

if (blobLogSink is not null)
{
    loggerConfiguration.WriteTo.Sink(blobLogSink);
}

Log.Logger = loggerConfiguration.CreateLogger();

builder.Host.UseSerilog();

if (blobLogOptions.Enabled && blobLogSink is null)
{
    Log.Warning("Blob logging is enabled but AzureBlob:ConnectionString is not configured; continuing with console logging only.");
}

// 2. Add Database Context
builder.Services.AddDbContext<SpeedClaimDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// 3. Configure JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["Secret"];
if (string.IsNullOrWhiteSpace(secretKey))
    throw new InvalidOperationException("JwtSettings:Secret must be configured in appsettings.");

builder.Services.AddAuthentication(options =>
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
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
    };
    options.Events = new JwtBearerEvents
    {
        OnTokenValidated = async context =>
        {
            var sessionClaim = context.Principal?.FindFirst("sid")?.Value
                ?? context.Principal?.FindFirst(ClaimTypes.Sid)?.Value;
            var userIdClaim = context.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!Guid.TryParse(sessionClaim, out var sessionId) || !Guid.TryParse(userIdClaim, out var userId))
            {
                context.Fail("Invalid session");
                return;
            }

            var unitOfWork = context.HttpContext.RequestServices.GetRequiredService<IUnitOfWork>();
            var session = await unitOfWork.Sessions.GetByIdAsync(sessionId);
            if (session == null || session.UserId != userId || session.IsRevoked || session.ExpiresAt <= DateTime.UtcNow)
            {
                context.Fail("Invalid session");
                return;
            }

            var user = await unitOfWork.Users.GetByIdAsync(userId);
            var tokenRole = context.Principal?.FindFirst(ClaimTypes.Role)?.Value;
            if (user == null || !user.IsActive || user.Role.ToString() != tokenRole)
            {
                context.Fail("Invalid user");
            }
        },
        OnMessageReceived = context =>
        {
            // SignalR's browser client can't attach an Authorization header to the
            // WebSocket/SSE handshake, so it sends the token as a query param instead.
            var accessToken = context.Request.Query["access_token"];
            if (!string.IsNullOrEmpty(accessToken) && context.HttpContext.Request.Path.StartsWithSegments("/hubs"))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        },
        OnChallenge = async context =>
        {
            context.HandleResponse();
            context.Response.StatusCode = 401;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new { error = "Authentication required. Please provide a valid JWT token." });
        },
        OnForbidden = async context =>
        {
            context.Response.StatusCode = 403;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new { error = "Access denied. You do not have permission to access this resource." });
        }
    };
});

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

builder.Services.AddSignalR();

// Add FluentValidation
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// Add Repositories and UnitOfWork
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IPolicyRepository, PolicyRepository>();
builder.Services.AddScoped<IClaimRepository, ClaimRepository>();
    builder.Services.AddScoped<IPremiumPaymentRepository, PremiumPaymentRepository>();
builder.Services.AddScoped<ISubmittedDocumentRepository, SubmittedDocumentRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Idempotency: in-memory distributed cache (swap for Redis in production)
builder.Services.AddDistributedMemoryCache();

// Add DI Services
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<IEncryptionService, EncryptionService>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IProductService, SpeedClaim.Api.Services.ProductService>();
builder.Services.AddScoped<IProductBrochureService, ProductBrochureService>();
builder.Services.AddScoped<IPolicyAssistantService, PolicyAssistantService>();
builder.Services.AddScoped<ISpeedyAssistantService, SpeedyAssistantService>();
builder.Services.AddScoped<IPolicyService, PolicyService>();
builder.Services.AddScoped<IClaimService, ClaimService>();
builder.Services.AddScoped<IFinanceService, FinanceService>();
builder.Services.AddScoped<IAgentService, AgentService>();
builder.Services.AddScoped<IProposalService, ProposalService>();
builder.Services.AddScoped<ISystemService, SystemService>();
builder.Services.AddScoped<IGrievanceService, GrievanceService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IStripeWrapper, StripeWrapper>();

// Infrastructure Services
builder.Services.AddSingleton<ISmtpClientFactory, SmtpClientFactory>();
if (string.Equals(builder.Configuration["EmailDelivery:Provider"], "ServiceBus", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddSingleton<IEmailDispatchQueue, ServiceBusEmailDispatchQueue>();
}
builder.Services.AddTransient<IEmailService, EmailService>();
builder.Services.AddScoped<IStorageService>(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var provider = configuration["Storage:Provider"];
    return string.Equals(provider, "AzureBlob", StringComparison.OrdinalIgnoreCase)
        ? ActivatorUtilities.CreateInstance<AzureBlobStorageService>(sp)
        : ActivatorUtilities.CreateInstance<LocalStorageService>(sp);
});
builder.Services.Configure<AiServiceOptions>(
    builder.Configuration.GetSection(AiServiceOptions.SectionName));
builder.Services.AddHttpClient<IBrochureIngestionClient, FastApiBrochureIngestionClient>()
    .RedactLoggedHeaders(headerName =>
        string.Equals(headerName, "X-Internal-Api-Key", StringComparison.OrdinalIgnoreCase));
builder.Services.AddHttpClient<IPolicyQaClient, FastApiPolicyQaClient>()
    .RedactLoggedHeaders(headerName =>
        string.Equals(headerName, "X-Internal-Api-Key", StringComparison.OrdinalIgnoreCase));
builder.Services.AddHttpClient<ISpeedyAssistantClient, FastApiSpeedyAssistantClient>()
    .RedactLoggedHeaders(headerName =>
        string.Equals(headerName, "X-Internal-Api-Key", StringComparison.OrdinalIgnoreCase));
builder.Services.AddHttpClient<ISpeedyWorkspaceClient, FastApiSpeedyWorkspaceClient>()
    .RedactLoggedHeaders(headerName =>
        string.Equals(headerName, "X-Internal-Api-Key", StringComparison.OrdinalIgnoreCase));



// Configure API Versioning
builder.Services.AddApiVersioning(options =>
{
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.ReportApiVersions = true;
})
.AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});

// Add OpenAPI via Swashbuckle
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "SpeedClaim API",
        Version = "v1",
        Description = "Insurance claims management platform API"
    });

    // Include XML doc comments
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (System.IO.File.Exists(xmlPath))
        options.IncludeXmlComments(xmlPath);

    // Auto-document roles and 401/403 from [Authorize] attributes
    options.OperationFilter<AuthorizeOperationFilter>();
    options.OperationFilter<IdempotencyOperationFilter>();

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT Authorization header using the Bearer scheme."
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// CORS — locked to Angular dev server; update AllowedOrigins in appsettings for production
var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>()
    ?? new[] { "http://localhost:4200" };

// Uploaded documents are rendered inside the trusted SpeedClaim portals. Keep this
// list derived from the same explicit origins used by CORS rather than allowing an
// arbitrary site to embed an identity document.
var uploadFrameAncestors = string.Join(" ", allowedOrigins
    .Select(origin => origin.TrimEnd('/'))
    .Where(origin => Uri.TryCreate(origin, UriKind.Absolute, out var uri)
        && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp))
    .Distinct(StringComparer.OrdinalIgnoreCase));

builder.Services.AddCors(options =>
{
    options.AddPolicy("SpeedClaimPolicy", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Rate Limiting
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.ContentType = "application/json";
        await context.HttpContext.Response.WriteAsJsonAsync(new
        {
            status = 429,
            message = "Too many requests. Please try again later."
        }, cancellationToken);
    };

    // Strict policy for auth endpoints: 10 requests per 60s per IP
    options.AddFixedWindowLimiter("auth", limiterOptions =>
    {
        limiterOptions.PermitLimit = 10;
        limiterOptions.Window = TimeSpan.FromSeconds(60);
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit = 0;
    });

    // General policy for all other endpoints: 100 requests per 60s per IP
    options.AddFixedWindowLimiter("api", limiterOptions =>
    {
        limiterOptions.PermitLimit = 100;
        limiterOptions.Window = TimeSpan.FromSeconds(60);
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit = 0;
    });

    options.AddFixedWindowLimiter("policy-qa", limiterOptions =>
    {
        limiterOptions.PermitLimit = 20;
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.QueueLimit = 0;
    });

    // Partition both policies by IP address
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromSeconds(60),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }));
});

var app = builder.Build();

if (args.Contains("--migrate", StringComparer.OrdinalIgnoreCase))
{
    Log.Information("Applying pending EF Core migrations");
    await using var scope = app.Services.CreateAsyncScope();
    var database = scope.ServiceProvider.GetRequiredService<SpeedClaimDbContext>();
    await database.Database.MigrateAsync();
    Log.Information("EF Core migrations completed successfully");
    return;
}

StripeConfiguration.ApiKey = builder.Configuration.GetSection("Stripe")["SecretKey"];

// Configure the HTTP request pipeline.
app.UseSerilogRequestLogging();
app.UseMiddleware<GlobalExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "SpeedClaim API v1");
    });
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.MapGet("/uploads/{**fileId}", [AllowAnonymous] async (
    string fileId,
    IStorageService storageService,
    HttpContext context) =>
{
    var storageKey = $"uploads/{fileId}";
    var provider = new FileExtensionContentTypeProvider();
    if (!provider.TryGetContentType(storageKey, out var contentType))
        contentType = "application/octet-stream";

    // PDF viewers honour CSP frame-ancestors and would otherwise reject our
    // cross-origin Static Web App. X-Frame-Options cannot express an allowlist,
    // so CSP is the authoritative, origin-specific control for upload previews.
    context.Response.Headers.Remove("X-Frame-Options");
    context.Response.Headers["Content-Security-Policy"] =
        $"default-src 'none'; frame-ancestors 'self' {uploadFrameAncestors}";

    var stream = await storageService.GetFileAsync(storageKey);
    return Results.File(stream, contentType);
});

// Security response headers — applied to every response
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    context.Response.Headers["X-XSS-Protection"] = "0";
    context.Response.Headers["Permissions-Policy"] = "geolocation=(), microphone=(), camera=()";

    // Upload endpoints replace these values with a CSP allowlist that permits
    // only configured SpeedClaim frontends to embed an in-app document preview.
    if (!context.Request.Path.StartsWithSegments("/uploads", StringComparison.OrdinalIgnoreCase))
    {
        context.Response.Headers["X-Frame-Options"] = "DENY";
        context.Response.Headers["Content-Security-Policy"] = "default-src 'none'; frame-ancestors 'none'";
    }

    await next();
});

app.UseCors("SpeedClaimPolicy");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health/live", () => Results.Ok(new { status = "ok" })).AllowAnonymous();
app.MapGet("/health/ready", () => Results.Ok(new { status = "ready" })).AllowAnonymous();
app.MapControllers();
app.MapHub<NotificationHub>("/hubs/notifications");

app.Run();
