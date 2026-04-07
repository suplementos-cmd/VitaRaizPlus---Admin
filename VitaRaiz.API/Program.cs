using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using VitaRaiz.Application;
using VitaRaiz.Infrastructure;
using VitaRaiz.API.Services;
using NLog;
using NLog.Web;

// Configure NLog FIRST before anything else
var logger = LogManager.Setup().LoadConfigurationFromAppSettings().GetCurrentClassLogger();
logger.Info("========================================");
logger.Info($"VitaRaiz API Starting - {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
logger.Info("========================================");

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Configure logging with NLog
    builder.Logging.ClearProviders();
    builder.Logging.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace);
    builder.Host.UseNLog();

    // Add services to the container
    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);

// Register JWT Token Service
builder.Services.AddScoped<JwtTokenService>();

builder.Services.AddControllers();

// Configure JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"];
var issuer = jwtSettings["Issuer"];
var audience = jwtSettings["Audience"];

if (string.IsNullOrEmpty(secretKey))
    throw new InvalidOperationException("JWT SecretKey not configured in appsettings.json");
if (string.IsNullOrEmpty(issuer))
    throw new InvalidOperationException("JWT Issuer not configured in appsettings.json");
if (string.IsNullOrEmpty(audience))
    throw new InvalidOperationException("JWT Audience not configured in appsettings.json");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.MapInboundClaims = false; // Usar nombres JWT estándar (sub, name, etc.)
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = issuer,
        ValidAudience = audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
        ClockSkew = TimeSpan.FromMinutes(5) // Tolerancia de 5 minutos
    };
    
    // Add events for detailed logging
    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            Console.WriteLine($"[JWT] Authentication failed: {context.Exception.Message}");
            return Task.CompletedTask;
        },
        OnTokenValidated = context =>
        {
            var userName = context.Principal?.Identity?.Name ?? "Unknown";
            Console.WriteLine($"[JWT] Token validated successfully for user: {userName}");
            return Task.CompletedTask;
        },
        OnChallenge = context =>
        {
            Console.WriteLine($"[JWT] Challenge triggered: {context.Error}, {context.ErrorDescription}");
            // Skip the default logic to avoid redirect
            context.HandleResponse();
            
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/json";
            return context.Response.WriteAsJsonAsync(new
            {
                error = "Unauthorized",
                message = "A valid JWT token is required to access this resource",
                details = context.ErrorDescription
            });
        }
    };
});

builder.Services.AddAuthorization();

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowMobileApp", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
    
    options.AddPolicy("AllowWebPortal", policy =>
    {
        policy.WithOrigins(
                "http://localhost:5145",     // WebPortal HTTP (Development - dotnet run)
                "https://localhost:7115",    // WebPortal HTTPS (Development - dotnet run)
                "http://localhost:32733",    // WebPortal HTTP (IIS Express)
                "https://localhost:44305"    // WebPortal HTTPS (IIS Express)
              )
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
    
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Configure Swagger with JWT
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "VitaRaiz Sales API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
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

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// DISABLED: Causes issues with HTTP localhost requests during development
// app.UseHttpsRedirection();

// Servir archivos estáticos (fotos de productos, etc.)
app.UseStaticFiles();

// Ensure uploads directory exists
var uploadsPath = Path.Combine(app.Environment.WebRootPath ?? Path.Combine(app.Environment.ContentRootPath, "wwwroot"), "uploads", "products");
Directory.CreateDirectory(uploadsPath);

// Apply CORS - En desarrollo usamos AllowAll para facilitar testing
if (app.Environment.IsDevelopment())
{
    app.UseCors("AllowAll");
}
else
{
    // En producción, configurar orígenes específicos
    app.UseCors("AllowMobileApp");
}

// Usar middleware personalizado para normalizar tokens JWT (con o sin "Bearer")
app.UseMiddleware<VitaRaiz.API.Middleware.JwtMiddleware>();

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

logger.Info("Application configured successfully. Starting web host...");
app.Run();
logger.Info("Application stopped gracefully.");
}
catch (Exception ex)
{
    logger.Fatal(ex, "Application terminated unexpectedly");
    throw;
}
finally
{
    LogManager.Shutdown();
}
