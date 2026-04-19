using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Serilog.Events;
using YepPet.Infrastructure.Auth;
using YepPet.Application;
using YepPet.Api.Endpoints;
using YepPet.Infrastructure;

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

    var envDir = Environment.GetEnvironmentVariable("YEPPET_LOGS_DIR");
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
    var path = Path.Combine(logsDir, "yeppet-.log");
    loggerConfiguration.WriteTo.File(
        path,
        restrictedToMinimumLevel: LogEventLevel.Debug,
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 31,
        shared: true,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}");
    Console.Error.WriteLine($"[YepPet] Serilog escriu logs a: {logsDir} (fitxers yeppet-YYYYMMDD.log)");
}

var builder = WebApplication.CreateBuilder(args);
var linkedInLocalConfig = Path.GetFullPath(
    Path.Combine(builder.Environment.ContentRootPath, "..", "..", "..", "config", "linkedin", "yeppet-dev.json"));
var facebookLocalConfig = Path.GetFullPath(
    Path.Combine(builder.Environment.ContentRootPath, "..", "..", "..", "config", "facebook", "yeppet-dev.json"));

builder.Configuration.AddJsonFile(linkedInLocalConfig, optional: true, reloadOnChange: true);
builder.Configuration.AddJsonFile(facebookLocalConfig, optional: true, reloadOnChange: true);

builder.Host.UseSerilog((context, services, loggerConfiguration) =>
{
    var fileLogsEnabled = context.HostingEnvironment.IsDevelopment()
        || context.Configuration.GetValue("YepPet:WriteLogsToFile", false)
        || string.Equals(
            Environment.GetEnvironmentVariable("YEPPET_FILE_LOGS"),
            "true",
            StringComparison.OrdinalIgnoreCase);

    Console.Error.WriteLine(
        "[YepPet] Serilog: Environment={0} Development={1} YepPet:WriteLogsToFile={2} → fitxer activat={3}",
        context.HostingEnvironment.EnvironmentName,
        context.HostingEnvironment.IsDevelopment(),
        context.Configuration.GetValue("YepPet:WriteLogsToFile", false),
        fileLogsEnabled);

    loggerConfiguration
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Application", "YepPet.Api")
        .Enrich.WithProperty("Environment", context.HostingEnvironment.EnvironmentName);

    if (fileLogsEnabled)
    {
        Serilog.Debugging.SelfLog.Enable(msg => Console.Error.WriteLine($"[Serilog SelfLog] {msg}"));
        try
        {
            var logsDir = ApiProjectLogsDirectory();
            Console.Error.WriteLine(
                "[YepPet] BaseDirectory={0} CurrentDirectory={1} → logsDir={2}",
                AppContext.BaseDirectory,
                Directory.GetCurrentDirectory(),
                logsDir);

            Directory.CreateDirectory(logsDir);
            var marker = Path.Combine(logsDir, $"yeppet-boot-{DateTime.UtcNow:yyyyMMddHHmmss}.txt");
            File.WriteAllText(
                marker,
                $"Arrencada OK {DateTime.UtcNow:o}{Environment.NewLine}PID {Environment.ProcessId}{Environment.NewLine}");
            Console.Error.WriteLine($"[YepPet] Prova d’escriptura: creat {marker}");

            AddApiFileSink(loggerConfiguration, logsDir);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[YepPet] Error preparant logs a disc: {ex}");
        }
    }
    else
    {
        Console.Error.WriteLine(
            "[YepPet] Logs a disc desactivats. Per activar-los: ASPNETCORE_ENVIRONMENT=Development, o YepPet:WriteLogsToFile=true, o YEPPET_FILE_LOGS=true.");
    }

    loggerConfiguration
        .ReadFrom.Configuration(context.Configuration)
        .WriteTo.Console(
            outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}");
});

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration, builder.Environment);
builder.Services.AddProblemDetails();
var authOptions = builder.Configuration.GetSection(AuthOptions.SectionName).Get<AuthOptions>() ?? new AuthOptions();
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
            OnAuthenticationFailed = context =>
            {
                Log.Warning(context.Exception, "JWT: autenticació fallida");
                return Task.CompletedTask;
            }
        };
    });
builder.Services.AddAuthorization();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "YepPet API",
        Version = "v1"
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

var apiLogsPath = ApiProjectLogsDirectory();
var fileLogsActive = app.Environment.IsDevelopment()
    || app.Configuration.GetValue("YepPet:WriteLogsToFile", false)
    || string.Equals(Environment.GetEnvironmentVariable("YEPPET_FILE_LOGS"), "true", StringComparison.OrdinalIgnoreCase);

Log.Information(
    "YepPet API iniciada; entorn {Environment}; logs a disc (Backend/Api/logs): actius={FileLogs}; directori={ApiLogs} (patró: yeppet-YYYYMMDD.log); ContentRoot={ContentRoot}",
    app.Environment.EnvironmentName,
    fileLogsActive,
    fileLogsActive ? apiLogsPath : "(desactivat)",
    app.Environment.ContentRootPath);

app.UseSerilogRequestLogging(options =>
{
    options.MessageTemplate =
        "HTTP {RequestMethod} {RequestPath}{Query} → {StatusCode} en {Elapsed:0.0000} ms [trace {TraceId}]";
    options.GetLevel = (httpContext, elapsed, ex) => ex != null
        ? LogEventLevel.Error
        : httpContext.Response.StatusCode > 499
            ? LogEventLevel.Error
            : LogEventLevel.Information;
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        diagnosticContext.Set("Query", httpContext.Request.QueryString.Value ?? string.Empty);
        diagnosticContext.Set("TraceId", httpContext.TraceIdentifier);
        diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value ?? string.Empty);
        diagnosticContext.Set("UserAgent", httpContext.Request.Headers.UserAgent.ToString());
        diagnosticContext.Set("RemoteIP", httpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty);
        diagnosticContext.Set("ContentLength", httpContext.Request.ContentLength ?? 0L);
    };
});

await using (var scope = app.Services.CreateAsyncScope())
{
    var seeder = scope.ServiceProvider.GetRequiredService<DevelopmentIdentitySeeder>();
    await seeder.SeedAsync();
}

app.UseExceptionHandler();
app.UseCors("web");
app.UseAuthentication();
app.UseAuthorization();
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "YepPet API v1");
    options.RoutePrefix = "swagger";
});

app.MapGet("/", () => "Hello World!");
app.MapGet("/health/db", () => Results.Ok(new { status = "configured" }));
app.MapAuthEndpoints();
app.MapNavigationEndpoints();
app.MapAdminEndpoints();
app.MapGeographicAdminEndpoints();
app.MapPlaceEndpoints();
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
