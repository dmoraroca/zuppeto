using System.IdentityModel.Tokens.Jwt;
using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.OpenApi;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Serilog.Events;
using Zuppeto.Infrastructure.Auth;
using Zuppeto.Application;
using Zuppeto.Application.Auth;
using Zuppeto.Application.Places;
using Zuppeto.Api.Endpoints;
using Zuppeto.Api.Observability;
using Zuppeto.Infrastructure;
using Zuppeto.Infrastructure.Persistence;
using Zuppeto.Domain.Abstractions;
using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Zuppeto.Api.Startup;

/// <summary>Carpeta <c>logs</c> al costat de <c>Api.csproj</c> (no depèn del ContentRoot).</summary>
static string ApiProjectLogsDirectory()
{
    static string? FindUpwards(string startDir, string markerFile)
    {
        var dir = new DirectoryInfo(startDir);
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir.FullName, markerFile)))
                return dir.FullName;
            dir = dir.Parent;
        }

        return null;
    }

    var envDir = Environment.GetEnvironmentVariable("ZUPPETO_LOGS_DIR");
    if (!string.IsNullOrWhiteSpace(envDir))
        return Path.GetFullPath(envDir.Trim());

    var entry = Assembly.GetEntryAssembly()?.Location;
    var entryDir = string.IsNullOrEmpty(entry) ? null : Path.GetDirectoryName(entry);

    var projectDir = FindUpwards(AppContext.BaseDirectory, "Api.csproj")
        ?? (entryDir != null ? FindUpwards(entryDir, "Api.csproj") : null)
        ?? FindUpwards(Directory.GetCurrentDirectory(), "Api.csproj");
    return projectDir != null
        ? Path.Combine(projectDir, "logs")
        : Path.Combine(Directory.GetCurrentDirectory(), "logs");
}

static void AddApiFileSink(LoggerConfiguration loggerConfiguration, string logsDir)
{
    Directory.CreateDirectory(logsDir);
    var path = Path.Combine(logsDir, "zuppeto-.log");
    loggerConfiguration.WriteTo.File(
        path,
        restrictedToMinimumLevel: LogEventLevel.Debug,
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 31,
        fileSizeLimitBytes: 25 * 1024 * 1024,
        rollOnFileSizeLimit: true,
        shared: true,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {SourceContext}: {Message:lj} {Properties:j}{NewLine}{Exception}");
    Console.Error.WriteLine($"[PetiLoc] Serilog escriu logs a: {logsDir} (fitxers zuppeto-YYYYMMDD.log)");
}

var builder = WebApplication.CreateBuilder(args);
var facebookLocalConfig = Path.GetFullPath(
    Path.Combine(builder.Environment.ContentRootPath, "..", "..", "..", "config", "facebook", "zuppeto-dev.json"));

builder.Configuration.AddJsonFile(facebookLocalConfig, optional: true, reloadOnChange: true);

builder.Host.UseSerilog((context, services, loggerConfiguration) =>
{
    var fileLogsEnabled = context.HostingEnvironment.IsDevelopment()
        || context.Configuration.GetValue("Zuppeto:WriteLogsToFile", false)
        || string.Equals(
            Environment.GetEnvironmentVariable("ZUPPETO_FILE_LOGS"),
            "true",
            StringComparison.OrdinalIgnoreCase);

    Console.Error.WriteLine(
        "[PetiLoc] Serilog: Environment={0} Development={1} Zuppeto:WriteLogsToFile={2} → fitxer activat={3}",
        context.HostingEnvironment.EnvironmentName,
        context.HostingEnvironment.IsDevelopment(),
        context.Configuration.GetValue("Zuppeto:WriteLogsToFile", false),
        fileLogsEnabled);

    loggerConfiguration
        .Enrich.FromLogContext()
        .Enrich.With<SensitiveLogPropertyEnricher>()
        .Enrich.WithProperty("Application", "Zuppeto.Api")
        .Enrich.WithProperty("Environment", context.HostingEnvironment.EnvironmentName);

    if (fileLogsEnabled)
    {
        Serilog.Debugging.SelfLog.Enable(msg => Console.Error.WriteLine($"[Serilog SelfLog] {msg}"));
        try
        {
            var logsDir = ApiProjectLogsDirectory();
            Console.Error.WriteLine(
                "[PetiLoc] BaseDirectory={0} CurrentDirectory={1} → logsDir={2}",
                AppContext.BaseDirectory,
                Directory.GetCurrentDirectory(),
                logsDir);

            AddApiFileSink(loggerConfiguration, logsDir);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[PetiLoc] Error preparant logs a disc: {ex}");
        }
    }
    else
    {
        Console.Error.WriteLine(
            "[PetiLoc] Logs a disc desactivats. Per activar-los: ASPNETCORE_ENVIRONMENT=Development, o Zuppeto:WriteLogsToFile=true, o ZUPPETO_FILE_LOGS=true.");
    }

    loggerConfiguration
        .ReadFrom.Configuration(context.Configuration)
        .WriteTo.Console(
            outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj} {Properties:j}{NewLine}{Exception}");
});

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration, builder.Environment);
builder.Services.AddHttpContextAccessor();
builder.Services.AddProblemDetails();
var authOptions = AuthOptionsFactory.Create(builder.Configuration);
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = authOptions.Jwt.Issuer,
            ValidAudience = authOptions.Jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(authOptions.Jwt.SigningKey)),
            NameClaimType = "sub",
            RoleClaimType = "role"
        };
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var subject = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? context.Principal?.FindFirstValue("sub");
                var tokenVersion = context.Principal?.FindFirstValue("security_version");
                if (!Guid.TryParse(subject, out var userId) || !int.TryParse(tokenVersion, out var version))
                {
                    context.Fail("JWT de sessió invàlid.");
                    return;
                }
                var users = context.HttpContext.RequestServices.GetRequiredService<IUserRepository>();
                var user = await users.GetByIdAsync(userId, context.HttpContext.RequestAborted);
                if (user is null || user.SecurityVersion != version)
                {
                    context.Fail("La sessió ja no és vigent.");
                    return;
                }

                var tokenId = context.Principal?.FindFirstValue(JwtRegisteredClaimNames.Jti);
                if (string.IsNullOrWhiteSpace(tokenId)) return;
                var revocations = context.HttpContext.RequestServices.GetRequiredService<IAccessTokenRevocationStore>();
                if (await revocations.IsRevokedAsync(tokenId, context.HttpContext.RequestAborted))
                {
                    context.Fail("La sessió ha estat tancada.");
                }
            },
            OnAuthenticationFailed = context =>
            {
                Log.Warning("JWT: autenticació fallida ({ExceptionType})", context.Exception.GetType().Name);
                return Task.CompletedTask;
            }
        };
    });
builder.Services.AddAuthorization();
builder.Services.AddRateLimiter(options => options.AddPolicy("totp", context =>
    RateLimitPartition.GetFixedWindowLimiter(context.Connection.RemoteIpAddress?.ToString() ?? "unknown", _ => new FixedWindowRateLimiterOptions
    { PermitLimit = 8, Window = TimeSpan.FromMinutes(1), QueueLimit = 0, AutoReplenishment = true })));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "PetiLoc API",
        Version = "v1"
    });

    // HTTP bearer scheme: do not set In/Name (OpenAPI 3); Swagger UI shows Authorize and sends Authorization header.
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description =
            "JWT from POST /api/auth/login. Paste only the token (Swagger adds the Bearer prefix)."
    });

    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = new List<string>()
    });
});
builder.Services.AddCors(options =>
{
    options.AddPolicy("web", policy =>
    {
        policy
            .WithOrigins("http://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

var databaseMaintenanceMode = DatabaseMaintenanceRequest.Parse(args);
if (databaseMaintenanceMode != DatabaseMaintenanceMode.None)
{
    await DatabaseMaintenanceCommand.RunAsync(databaseMaintenanceMode, app.Services);
    Log.CloseAndFlush();
    return;
}

var apiLogsPath = ApiProjectLogsDirectory();
var fileLogsActive = app.Environment.IsDevelopment()
    || app.Configuration.GetValue("Zuppeto:WriteLogsToFile", false)
    || string.Equals(Environment.GetEnvironmentVariable("ZUPPETO_FILE_LOGS"), "true", StringComparison.OrdinalIgnoreCase);

Log.Information(
    "PetiLoc API iniciada; entorn {Environment}; logs a disc (Backend/Api/logs): actius={FileLogs}; directori={ApiLogs} (patró: zuppeto-YYYYMMDD.log); ContentRoot={ContentRoot}",
    app.Environment.EnvironmentName,
    fileLogsActive,
    fileLogsActive ? apiLogsPath : "(desactivat)",
    app.Environment.ContentRootPath);

app.UseMiddleware<RequestCorrelationMiddleware>();
app.UseSerilogRequestLogging(options =>
{
    options.MessageTemplate =
        "HTTP {RequestMethod} {RequestPath} → {StatusCode} en {Elapsed:0.0000} ms [trace {TraceId}] [correlation {CorrelationId}] [ZUP {ZupTestCode}] [rol {TestRole}] [navegador {TestBrowser}]";
    options.GetLevel = (httpContext, elapsed, ex) => ex != null
        ? LogEventLevel.Error
        : httpContext.Response.StatusCode > 499
            ? LogEventLevel.Error
            : LogEventLevel.Information;
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        diagnosticContext.Set("TraceId", httpContext.TraceIdentifier);
        diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value ?? string.Empty);
        diagnosticContext.Set("UserAgent", httpContext.Request.Headers.UserAgent.ToString());
        diagnosticContext.Set("RemoteIP", httpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty);
        diagnosticContext.Set("ContentLength", httpContext.Request.ContentLength ?? 0L);
    };
});

app.UseExceptionHandler();
app.UseCors("web");
app.UseAuthentication();
app.UseMiddleware<RequestLogEnrichmentMiddleware>();
app.UseAuthorization();
app.UseRateLimiter();

var mediaRoot = Path.Combine(app.Environment.ContentRootPath, "storage");
Directory.CreateDirectory(Path.Combine(mediaRoot, "place-covers"));
Directory.CreateDirectory(Path.Combine(mediaRoot, "place-media"));
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(mediaRoot),
    RequestPath = "/media"
});
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "PetiLoc API v1");
    options.RoutePrefix = "swagger";
    options.EnablePersistAuthorization();
});

app.MapGet("/", () => "Hello World!");
app.MapGet("/health/db", () => Results.Ok(new { status = "configured" }));
app.MapAuthEndpoints();
app.MapDevelopmentActivationTestEndpoints();
app.MapNavigationEndpoints();
app.MapAdminEndpoints();
app.MapGeographicAdminEndpoints();
app.MapTerritorialAdminEndpoints();
app.MapTerritorialLocationEndpoints();
app.MapPlaceEndpoints();
app.MapPlaceMediaEndpoints();
app.MapPetGovernanceEndpoints();
app.MapPlaceImportEndpoints();
app.MapPlaceCatalogGovernanceEndpoints();
app.MapExternalIntelligenceEndpoints();
app.MapFavoriteEndpoints();

app.MapUserEndpoints();
app.MapReviewEndpoints();

try
{
    app.Run();
}
finally
{
    Log.CloseAndFlush();
}
