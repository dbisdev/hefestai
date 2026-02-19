using Loremaster.Application;
using Loremaster.Infrastructure;
using Loremaster.Api.Middleware;
using Loremaster.Api.Services;
using Loremaster.Application.Common.Interfaces;
using Loremaster.Infrastructure.Persistence;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Reflection;
using System.Threading.RateLimiting;

// Configure Serilog early
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(new ConfigurationBuilder()
        .AddJsonFile("appsettings.json")
        .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true)
        .AddEnvironmentVariables()
        .Build())
    .Enrich.FromLogContext()
    .Enrich.WithEnvironmentName()
    .Enrich.WithThreadId()
    .Enrich.WithCorrelationId()
    .CreateLogger();

try
{
    Log.Information("Starting Loremaster API");

    var builder = WebApplication.CreateBuilder(args);

    // Use Serilog
    builder.Host.UseSerilog();

    // Add services
    builder.Services.AddApplicationServices();
    builder.Services.AddInfrastructureServices(builder.Configuration, builder.Environment.EnvironmentName);

    // Current user service
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

    // Rate Limiting
    var rateLimitConfig = builder.Configuration.GetSection("RateLimiting");
    if (rateLimitConfig.GetValue<bool>("EnableRateLimiting"))
    {
        builder.Services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            
            // Fixed window limiter for general API
            options.AddFixedWindowLimiter("fixed", opt =>
            {
                opt.PermitLimit = rateLimitConfig.GetValue<int>("PermitLimit", 100);
                opt.Window = TimeSpan.FromSeconds(rateLimitConfig.GetValue<int>("WindowSeconds", 60));
                opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                opt.QueueLimit = rateLimitConfig.GetValue<int>("QueueLimit", 10);
            });

            // Stricter limiter for auth endpoints
            options.AddFixedWindowLimiter("auth", opt =>
            {
                opt.PermitLimit = 10;
                opt.Window = TimeSpan.FromMinutes(1);
                opt.QueueLimit = 0;
            });

            // Per-user limiter
            options.AddPolicy("per-user", context =>
            {
                var userId = context.User?.FindFirst("sub")?.Value ?? "anonymous";
                return RateLimitPartition.GetFixedWindowLimiter(
                    userId,
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = rateLimitConfig.GetValue<int>("PermitLimit", 100),
                        Window = TimeSpan.FromSeconds(rateLimitConfig.GetValue<int>("WindowSeconds", 60)),
                        QueueLimit = 5
                    });
            });

            options.OnRejected = async (context, cancellationToken) =>
            {
                Log.Warning("Rate limit exceeded for {Path} from {IP}",
                    context.HttpContext.Request.Path,
                    context.HttpContext.Connection.RemoteIpAddress);

                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                await context.HttpContext.Response.WriteAsJsonAsync(new
                {
                    error = "Too Many Requests",
                    message = "Rate limit exceeded. Please try again later.",
                    retryAfter = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter)
                        ? retryAfter.TotalSeconds
                        : 60
                }, cancellationToken);
            };
        });
    }

    // Health Checks
    var isDevelopment = builder.Environment.IsDevelopment();
    var connectionString = isDevelopment
        ? builder.Configuration.GetConnectionString("DefaultConnection")
        : builder.Configuration.GetConnectionString("SupabaseConnection") 
          ?? builder.Configuration.GetConnectionString("DefaultConnection");
    var healthChecksBuilder = builder.Services.AddHealthChecks();
    
    if (!string.IsNullOrEmpty(connectionString))
    {
        healthChecksBuilder.AddNpgSql(
            connectionString,
            name: "postgresql",
            tags: new[] { "db", "sql", "postgresql" });
    }
    
    var genkitBaseUrl = isDevelopment
        ? builder.Configuration["GenkitService:BaseUrl"]
        : builder.Configuration["GenkitService:ProductionUrl"] ?? builder.Configuration["GenkitService:BaseUrl"];
    if (!string.IsNullOrEmpty(genkitBaseUrl))
    {
        healthChecksBuilder.AddUrlGroup(
            new Uri(genkitBaseUrl + "/health"),
            name: "genkit-service",
            tags: new[] { "ai", "genkit" });
    }

    // Controllers
    builder.Services.AddControllers()
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
            options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
        });

    // Swagger
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo 
        { 
            Title = "HefestAi API", 
            Version = "v1",
            Description = "API for HefestAi application - AI-powered worldbuilding and lore management"
        });

        // Set the comments path for the Swagger JSON and UI.
        var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
        c.IncludeXmlComments(xmlPath);
        
        c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
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

    // CORS
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowFrontend", policy =>
        {
            var origins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>() 
                ?? new[] { "http://localhost:5173" };
            
            policy.WithOrigins(origins)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials()
                .WithExposedHeaders("X-Pagination", "X-Request-Id");
        });
    });

    var app = builder.Build();

    // Seed database with initial data (admin user)
    using (var scope = app.Services.CreateScope())
    {
        var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
        await seeder.SeedAsync();
    }

    // Middleware pipeline (order matters!)
    
    // 1. Exception handling (first to catch all errors)
    app.UseMiddleware<ExceptionHandlingMiddleware>();
    
    // 2. Request logging
    app.UseSerilogRequestLogging(options =>
    {
        options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
        {
            diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
            diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
            diagnosticContext.Set("UserAgent", httpContext.Request.Headers.UserAgent.ToString());
            diagnosticContext.Set("ClientIP", httpContext.Connection.RemoteIpAddress?.ToString());
            
            if (httpContext.User.Identity?.IsAuthenticated == true)
            {
                diagnosticContext.Set("UserId", httpContext.User.FindFirst("sub")?.Value);
            }
        };
    });

    // 3. Security headers
    app.Use(async (context, next) =>
    {
        context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
        context.Response.Headers.Append("X-Frame-Options", "DENY");
        context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
        context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
        
        // Content Security Policy
        var csp = "default-src 'self'; " +
                   "script-src 'self' 'unsafe-inline' 'unsafe-eval' https://cdn.jsdelivr.net; " +
                   "style-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net; " +
                   "img-src 'self' data: blob: https:; " +
                   "font-src 'self' data:; " +
                   "connect-src 'self' https://generativelanguage.googleapis.com; " +
                   "frame-src 'none'; " +
                   "object-src 'none'; " +
                   "base-uri 'self';";
        
        context.Response.Headers.Append("Content-Security-Policy", csp);
        
        if (!app.Environment.IsDevelopment())
        {
            context.Response.Headers.Append("Strict-Transport-Security", "max-age=31536000; includeSubDomains");
        }
        
        await next();
    });

    // 4. Swagger (development only)
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "HefestAi API v1");
            c.RoutePrefix = "swagger";
        });
    }

    // 5. HTTPS Redirection (skip in development for Docker)
    if (!app.Environment.IsDevelopment())
    {
        app.UseHttpsRedirection();
    }

    // 6. CORS
    app.UseCors("AllowFrontend");

    // 7. Static files (favicon, etc.)
    app.UseStaticFiles();

    // 8. Rate Limiting
    if (rateLimitConfig.GetValue<bool>("EnableRateLimiting"))
    {
        app.UseRateLimiter();
    }

    // 9. Authentication & Authorization
    app.UseAuthentication();
    app.UseAuthorization();

    // Health check endpoints (no auth required)
    app.MapHealthChecks("/health", new HealthCheckOptions
    {
        ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
    });

    app.MapHealthChecks("/health/ready", new HealthCheckOptions
    {
        Predicate = check => check.Tags.Contains("db"),
        ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
    });

    app.MapHealthChecks("/health/live", new HealthCheckOptions
    {
        Predicate = _ => false
    });

    // Map controllers with rate limiting
    app.MapControllers()
        .RequireRateLimiting("per-user");

    Log.Information("HefestAi API started successfully");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

// Partial class declaration to make Program accessible for integration tests
public partial class Program { }
