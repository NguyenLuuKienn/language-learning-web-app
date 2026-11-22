using System.Text;
using System.Security.Cryptography;
using System.Net;
using System.Security.Claims;
using System.Threading.RateLimiting;
using LanguageLearning.Api.Middleware;
using LanguageLearning.Api.Services;
using LanguageLearning.Application.Common;
using LanguageLearning.Application;
using LanguageLearning.Application.Interfaces;
using LanguageLearning.Infrastructure;
using LanguageLearning.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
if (!builder.Environment.IsDevelopment() && string.IsNullOrWhiteSpace(builder.Configuration.GetConnectionString("DefaultConnection")))
    throw new InvalidOperationException("ConnectionStrings:DefaultConnection must be configured.");

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();
var dataProtectionPath = builder.Configuration["Security:DataProtectionKeysPath"];
if (!string.IsNullOrWhiteSpace(dataProtectionPath))
{
    Directory.CreateDirectory(dataProtectionPath);
    builder.Services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo(dataProtectionPath));
}
else if (!builder.Environment.IsDevelopment())
    throw new InvalidOperationException("Security:DataProtectionKeysPath must be configured outside Development.");

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    })
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var problem = new ValidationProblemDetails(context.ModelState)
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Validation failed"
            };
            problem.Extensions["code"] = ErrorCodes.Validation;
            return new BadRequestObjectResult(problem);
        };
    });
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Language Learning Web API",
        Version = "v1",
        Description = "REST API for the Language Learning platform with Spaced Repetition (SRS) and interactive courses."
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Enter JWT Bearer token in the format: Bearer {your_token}",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
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

builder.Services.AddCors(options =>
{
    var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins")
        .GetChildren()
        .Select(item => item.Value)
        .Where(value => !string.IsNullOrWhiteSpace(value))
        .Cast<string>()
        .ToArray();
    if (allowedOrigins.Length == 0 && builder.Environment.IsDevelopment())
        allowedOrigins = ["http://localhost:5173", "http://127.0.0.1:5173", "http://localhost:5174", "http://127.0.0.1:5174", "http://localhost:3000"];
    options.AddDefaultPolicy(policy =>
    {
        if (allowedOrigins.Length > 0)
            policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod();
    });
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    var trustProxyClientIp = builder.Configuration.GetValue<bool>("Security:TrustProxyClientIp");
    options.AddPolicy("auth", context => RateLimitPartition.GetFixedWindowLimiter(
        trustProxyClientIp && IPAddress.TryParse(context.Request.Headers["X-Forwarded-For"].ToString(), out var clientIp)
            ? clientIp.ToString()
            : context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 20,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = true
        }));
    options.AddPolicy("session", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 120,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = true
        }));
    options.AddPolicy("placement", context => RateLimitPartition.GetFixedWindowLimiter(
        context.User.FindFirst("sub")?.Value
            ?? context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 5,
            Window = TimeSpan.FromMinutes(10),
            QueueLimit = 0,
            AutoReplenishment = true
        }));
    options.AddPolicy("upload", context => RateLimitPartition.GetFixedWindowLimiter(
        context.User.FindFirst("sub")?.Value
            ?? context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 30,
            Window = TimeSpan.FromHours(1),
            QueueLimit = 0,
            AutoReplenishment = true
        }));
});

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

if (builder.Environment.IsDevelopment() && string.IsNullOrWhiteSpace(builder.Configuration["Jwt:SecretKey"]))
    builder.Configuration["Jwt:SecretKey"] = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
var jwtSecretKey = builder.Configuration["Jwt:SecretKey"];
if (string.IsNullOrWhiteSpace(jwtSecretKey) || Encoding.UTF8.GetByteCount(jwtSecretKey) < 32)
    throw new InvalidOperationException("Jwt:SecretKey must be configured with at least 32 bytes.");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "LanguageLearningApp";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "LanguageLearningAppClient";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey)),
        ValidateIssuer = true,
        ValidIssuer = jwtIssuer,
        ValidateAudience = true,
        ValidAudience = jwtAudience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            if (string.IsNullOrEmpty(context.Token))
                context.Token = context.Request.Cookies["ll_access"];
            return Task.CompletedTask;
        },
        OnTokenValidated = async context =>
        {
            var idValue = context.Principal?.FindFirst("sub")?.Value
                ?? context.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(idValue, out var userId))
            {
                context.Fail("Invalid user identity.");
                return;
            }

            var db = context.HttpContext.RequestServices.GetRequiredService<LanguageLearning.Infrastructure.Data.AppDbContext>();
            var account = await db.Users.AsNoTracking()
                .Where(user => user.Id == userId)
                .Select(user => new { user.IsActive, user.Role, user.PasswordHash })
                .SingleOrDefaultAsync(context.HttpContext.RequestAborted);
            var tokenRole = context.Principal?.FindFirst(ClaimTypes.Role)?.Value;
            if (account == null || !account.IsActive || !string.Equals(tokenRole, account.Role.ToString(), StringComparison.Ordinal)
                || !AuthStamp.Matches(context.Principal?.FindFirst("auth_stamp")?.Value,
                    AuthStamp.Compute(userId, account.PasswordHash, jwtSecretKey)))
                context.Fail("User account is inactive or the session role has changed.");
        }
    };
});

builder.Services.AddAuthorization();

var app = builder.Build();

app.UseExceptionHandler();
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["Permissions-Policy"] = "camera=(), microphone=(self), geolocation=()";
    if (context.Request.Path.StartsWithSegments("/api/v1/auth"))
        context.Response.Headers.CacheControl = "no-store";
    await next();
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.Use(async (context, next) =>
{
    var method = context.Request.Method;
    var mutating = method != HttpMethods.Get && method != HttpMethods.Head && method != HttpMethods.Options;
    var publicAuth = context.Request.Path.StartsWithSegments("/api/v1/auth/login")
        || context.Request.Path.StartsWithSegments("/api/v1/auth/register");
    var hasSessionCookie = context.Request.Cookies.ContainsKey("ll_access")
        || context.Request.Cookies.ContainsKey("ll_refresh");
    if (mutating && !publicAuth && hasSessionCookie
        && !context.Request.Headers.ContainsKey("Authorization"))
    {
        var cookie = context.Request.Cookies["ll_csrf"];
        var header = context.Request.Headers["X-CSRF-Token"].ToString();
        if (string.IsNullOrEmpty(cookie) || string.IsNullOrEmpty(header)
            || cookie.Length != header.Length
            || !CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(cookie), Encoding.UTF8.GetBytes(header)))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return;
        }
    }
    await next();
});

app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();

app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    try
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<LanguageLearning.Infrastructure.Data.AppDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        dbContext.Database.EnsureCreated();
        LanguageLearning.Infrastructure.Data.LegacySchemaUpgrade.EnsureCourseCategoriesTableAsync(dbContext).GetAwaiter().GetResult();
        LanguageLearning.Infrastructure.Data.LegacySchemaUpgrade.EnsureNotebookTableAsync(dbContext).GetAwaiter().GetResult();
        LanguageLearning.Infrastructure.Data.LegacySchemaUpgrade.EnsureVocabularySessionTableAsync(dbContext).GetAwaiter().GetResult();
        LanguageLearning.Infrastructure.Data.LegacySchemaUpgrade.EnsurePracticeTablesAsync(dbContext).GetAwaiter().GetResult();
        LanguageLearning.Infrastructure.Data.LegacySchemaUpgrade.EnsurePlacementReviewTablesAsync(dbContext).GetAwaiter().GetResult();
        LanguageLearning.Infrastructure.Data.LegacySchemaUpgrade.EnsureCourseMaterialColumnsAsync(dbContext).GetAwaiter().GetResult();
        LanguageLearning.Infrastructure.Data.LegacySchemaUpgrade.EnsureVocabularyDetailColumnsAsync(dbContext).GetAwaiter().GetResult();
        LanguageLearning.Infrastructure.Data.LegacySchemaUpgrade.EnsureCardDetailColumnsAsync(dbContext).GetAwaiter().GetResult();
        LanguageLearning.Infrastructure.Data.LegacySchemaUpgrade.EnsureStudyTargetColumnAsync(dbContext).GetAwaiter().GetResult();
        LanguageLearning.Infrastructure.Data.LegacySchemaUpgrade.EnsureWritingSubmissionsTableAsync(dbContext).GetAwaiter().GetResult();
        LanguageLearning.Infrastructure.Data.LegacySchemaUpgrade.EnsureLessonJourneyProgressAsync(dbContext).GetAwaiter().GetResult();
        LanguageLearning.Infrastructure.Data.LegacySchemaUpgrade.EnsureCardLessonSourcesAsync(dbContext).GetAwaiter().GetResult();
        LanguageLearning.Infrastructure.Data.LegacySchemaUpgrade.EnsureLearningJourneyRecordsAsync(dbContext).GetAwaiter().GetResult();
        LanguageLearning.Infrastructure.Data.LegacySchemaUpgrade.EnsureReviewObservationColumnsAsync(dbContext).GetAwaiter().GetResult();
        LanguageLearning.Infrastructure.Data.LegacySchemaUpgrade.EnsureQuizAttemptKeyAsync(dbContext).GetAwaiter().GetResult();
        LanguageLearning.Infrastructure.Data.LegacySchemaUpgrade.EnsureSkillPracticeEventsAsync(dbContext).GetAwaiter().GetResult();
        LanguageLearning.Infrastructure.Data.LegacySchemaUpgrade.EnsureWritingSubmissionKeyAsync(dbContext).GetAwaiter().GetResult();

        var initialAdminPassword = builder.Configuration["Bootstrap:AdminPassword"]
            ?? (app.Environment.IsDevelopment() ? "Admin@123456" : null);
        LanguageLearning.Infrastructure.Data.DataSeeder.SeedAsync(dbContext, passwordHasher, initialAdminPassword).GetAwaiter().GetResult();
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogCritical(ex, "Database initialization failed. The API cannot serve requests until the schema is ready.");
        throw new InvalidOperationException("Database initialization failed.", ex);
    }
}

app.Run();

public partial class Program { }
