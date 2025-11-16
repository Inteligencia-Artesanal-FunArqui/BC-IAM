using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using OsitoPolar.IAM.Service.Domain.Repositories;
using OsitoPolar.IAM.Service.Domain.Services;
using OsitoPolar.IAM.Service.Application.Internal.CommandServices;
using OsitoPolar.IAM.Service.Application.Internal.QueryServices;
using OsitoPolar.IAM.Service.Application.Internal.OutboundServices;
using OsitoPolar.IAM.Service.Infrastructure.Persistence.EFC.Configuration;
using OsitoPolar.IAM.Service.Infrastructure.Persistence.EFC.Repositories;
using OsitoPolar.IAM.Service.Infrastructure.Hashing.BCrypt.Services;
using OsitoPolar.IAM.Service.Infrastructure.Tokens.JWT.Services;
using OsitoPolar.IAM.Service.Infrastructure.Tokens.JWT.Configuration;
using OsitoPolar.IAM.Service.Infrastructure.Security;
using OsitoPolar.IAM.Service.Shared.Infrastructure.Interfaces.ASP.Configuration;
using OsitoPolar.IAM.Service.Shared.Domain.Repositories;
using OsitoPolar.IAM.Service.Shared.Infrastructure.Persistence.EFC.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using OsitoPolar.IAM.Service.Shared.Interfaces.ACL;
using OsitoPolar.IAM.Service.Application.ACL.Services;

var builder = WebApplication.CreateBuilder(args);

// ===========================
// CORS Configuration
// ===========================
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAllPolicy",
        policy => policy.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader());
});

// ===========================
// Database Configuration
// ===========================
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

if (connectionString != null)
{
    builder.Services.AddDbContext<IAMDbContext>(options =>
    {
        options.UseMySQL(connectionString);
        options.EnableSensitiveDataLogging();
        options.EnableDetailedErrors();
    });

    // Register IAMDbContext as DbContext for generic repository pattern
    builder.Services.AddScoped<DbContext>(provider => provider.GetRequiredService<IAMDbContext>());
}

// ===========================
// JWT Configuration
// ===========================
builder.Services.Configure<TokenSettings>(builder.Configuration.GetSection("TokenSettings"));

var tokenSettings = builder.Configuration.GetSection("TokenSettings").Get<TokenSettings>();
var secret = tokenSettings?.Secret ?? throw new InvalidOperationException("JWT Secret not configured");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
        ClockSkew = TimeSpan.Zero
    };
});

// ===========================
// Dependency Injection - Repositories
// ===========================
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// ===========================
// Dependency Injection - Domain Services
// ===========================
builder.Services.AddScoped<IUserCommandService, UserCommandService>();
builder.Services.AddScoped<IUserQueryService, UserQueryService>();
builder.Services.AddScoped<IRegistrationService, RegistrationService>();

// ===========================
// Dependency Injection - Application Services
// ===========================
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IHashingService, HashingService>();
builder.Services.AddScoped<ITwoFactorService, TwoFactorService>();

// ===========================
// FASE 2: HTTP Facades for Microservices Communication
// ===========================
// Profiles Service - for creating Owner profiles
builder.Services.AddHttpClient<IProfilesContextFacade, ProfilesHttpFacade>(client =>
{
    var profilesUrl = builder.Configuration["ServiceUrls:ProfilesService"]
        ?? throw new InvalidOperationException("ProfilesService URL not configured");

    client.BaseAddress = new Uri(profilesUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.Add("User-Agent", "IAM-Service/1.0");
});

// Notifications Service - for sending emails
builder.Services.AddHttpClient<INotificationContextFacade, NotificationsHttpFacade>(client =>
{
    var notificationsUrl = builder.Configuration["ServiceUrls:NotificationsService"]
        ?? throw new InvalidOperationException("NotificationsService URL not configured");

    client.BaseAddress = new Uri(notificationsUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.Add("User-Agent", "IAM-Service/1.0");
});

// Subscriptions Service - for managing user plans
builder.Services.AddHttpClient<ISubscriptionContextFacade, SubscriptionsHttpFacade>(client =>
{
    var subscriptionsUrl = builder.Configuration["ServiceUrls:SubscriptionsService"]
        ?? throw new InvalidOperationException("SubscriptionsService URL not configured");

    client.BaseAddress = new Uri(subscriptionsUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.Add("User-Agent", "IAM-Service/1.0");
});

// ===========================
// Controllers Configuration
// ===========================
builder.Services.AddControllers(options =>
{
    options.Conventions.Add(new KebabCaseRouteNamingConvention());
});

// ===========================
// Swagger/OpenAPI Configuration
// ===========================
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "OsitoPolar IAM Service API",
        Version = "v1",
        Description = "IAM Microservice - Authentication & Authorization"
    });
    options.EnableAnnotations();

    // JWT Authentication in Swagger
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Please enter JWT with Bearer prefix (e.g., 'Bearer {token}')",
        Name = "Authorization",
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

// ===========================
// Build Application
// ===========================
var app = builder.Build();

// ===========================
// Verify Database Connection on Startup
// ===========================
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<IAMDbContext>();
    try
    {
        context.Database.CanConnect();
        Console.WriteLine("✅ Database connection successful");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Database connection failed: {ex.Message}");
    }
}

// ===========================
// Configure HTTP Request Pipeline
// ===========================
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAllPolicy");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

Console.WriteLine("🚀 IAM Service running on port 5001");

app.Run();