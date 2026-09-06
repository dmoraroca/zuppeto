using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Zuppeto.Application.Auth;
using Zuppeto.Application.Places;
using Zuppeto.Domain.Abstractions;
using Zuppeto.Infrastructure.Auth;
using Zuppeto.Infrastructure.GeoNames;
using Zuppeto.Infrastructure.GooglePlaces;
using Zuppeto.Infrastructure.Persistence;
using Zuppeto.Infrastructure.Persistence.Repositories;
using Zuppeto.Infrastructure.Places;
using Zuppeto.Infrastructure.RabbitMq;

namespace Zuppeto.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment hostEnvironment)
    {
        var connectionString = configuration.GetConnectionString("Zuppeto")
            ?? "Host=localhost;Port=5433;Database=zuppeto;Username=app;Password=app";
        var expiresInMinutes = int.TryParse(configuration["Auth:Jwt:ExpiresInMinutes"], out var parsedExpiresInMinutes)
            ? parsedExpiresInMinutes
            : 480;
        var authOptions = new AuthOptions
        {
            FrontendBaseUrl = configuration["Auth:FrontendBaseUrl"] ?? "http://localhost:4200",
            Jwt = new AuthOptions.JwtOptions
            {
                Issuer = configuration["Auth:Jwt:Issuer"] ?? "Zuppeto",
                Audience = configuration["Auth:Jwt:Audience"] ?? "Zuppeto.Web",
                SigningKey = configuration["Auth:Jwt:SigningKey"]
                    ?? "dev-only-zuppeto-signing-key-change-in-production-123456789",
                ExpiresInMinutes = expiresInMinutes
            },
            Google = new AuthOptions.GoogleOptions
            {
                ClientId = configuration["Auth:Google:ClientId"] ?? string.Empty,
                AdminEmails = configuration.GetSection("Auth:Google:AdminEmails")
                    .GetChildren()
                    .Select(section => section.Value)
                    .Where(value => !string.IsNullOrWhiteSpace(value))
                    .Select(value => value!)
                    .ToArray()
            },
            LinkedIn = new AuthOptions.LinkedInOptions
            {
                ClientId = configuration["Auth:LinkedIn:ClientId"] ?? string.Empty,
                ClientSecret = configuration["Auth:LinkedIn:ClientSecret"] ?? string.Empty,
                RedirectUri = configuration["Auth:LinkedIn:RedirectUri"] ?? string.Empty,
                AdminEmails = configuration.GetSection("Auth:LinkedIn:AdminEmails")
                    .GetChildren()
                    .Select(section => section.Value)
                    .Where(value => !string.IsNullOrWhiteSpace(value))
                    .Select(value => value!)
                    .ToArray()
            },
            Facebook = new AuthOptions.FacebookOptions
            {
                AppId = configuration["Auth:Facebook:AppId"] ?? string.Empty,
                AppSecret = configuration["Auth:Facebook:AppSecret"] ?? string.Empty,
                RedirectUri = configuration["Auth:Facebook:RedirectUri"] ?? string.Empty,
                AdminEmails = configuration.GetSection("Auth:Facebook:AdminEmails")
                    .GetChildren()
                    .Select(section => section.Value)
                    .Where(value => !string.IsNullOrWhiteSpace(value))
                    .Select(value => value!)
                    .ToArray()
            }
        };

        services.AddSingleton(Options.Create(authOptions));
        services.AddRabbitMq(configuration);
        services.AddMemoryCache();
        services.AddDataProtection();
        services.Configure<GeoNamesOptions>(configuration.GetSection(GeoNamesOptions.SectionName));
        services.Configure<GooglePlacesOptions>(configuration.GetSection(GooglePlacesOptions.SectionName));
        services.Configure<GooglePlacesComplianceOptions>(
            configuration.GetSection(GooglePlacesComplianceOptions.SectionName));
        services.AddHostedService<GooglePlacesComplianceRetentionHostedService>();
        services.AddSingleton<PlaceCoverEnrichmentQueue>();
        services.AddSingleton<IPlaceCoverEnrichmentQueue>(sp => sp.GetRequiredService<PlaceCoverEnrichmentQueue>());
        services.AddHostedService<PlaceCoverEnrichmentHostedService>();
        services.AddDbContext<ZuppetoDbContext>((sp, options) =>
        {
            options.UseNpgsql(connectionString);
            options.AddInterceptors(new ClampStringMaxLengthInterceptor());
            if (hostEnvironment.IsDevelopment())
            {
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
            }
        });
        services.AddHttpClient<ILinkedInOAuthClient, LinkedInOAuthClient>();
        services.AddHttpClient<IFacebookOAuthClient, FacebookOAuthClient>();
        services.AddHttpClient<IExternalCitySuggestionProvider, GeoNamesCitySuggestionProvider>((sp, client) =>
        {
            var geoOptions = sp.GetRequiredService<IOptions<GeoNamesOptions>>().Value;
            client.BaseAddress = new Uri(geoOptions.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(Math.Max(2, geoOptions.TimeoutSeconds));
        });
        services.AddHttpClient<GooglePlacesSuggestionProvider>((sp, client) =>
        {
            var placesOptions = sp.GetRequiredService<IOptions<GooglePlacesOptions>>().Value;
            client.BaseAddress = new Uri(placesOptions.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(Math.Max(20, placesOptions.TimeoutSeconds));
        })
        .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
        {
            AllowAutoRedirect = true,
            MaxAutomaticRedirections = 5
        });
        services.AddScoped<IExternalPlaceSuggestionProvider>(
            sp => sp.GetRequiredService<GooglePlacesSuggestionProvider>());
        services.AddScoped<IExternalPlaceDetailsProvider>(
            sp => sp.GetRequiredService<GooglePlacesSuggestionProvider>());
        services.AddHttpClient<IPlaceWebsitePageReader, HttpPlaceWebsitePageReader>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(8);
            client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "ZuppetoPlaceEnrichment/1.0");
        });
        services.AddSingleton<IPlaceCoverStorage, FilePlaceCoverStorage>();
        services.AddScoped<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddScoped<IAccessTokenIssuer, JwtAccessTokenIssuer>();
        services.AddScoped<IGoogleIdTokenVerifier, GoogleIdTokenVerifier>();
        services.AddScoped<DevelopmentIdentitySeeder>();
        services.AddScoped<DevelopmentPlacesSeeder>();
        services.AddScoped<IMenuRepository, MenuRepository>();
        services.AddScoped<IPlaceRepository, PlaceRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRolePermissionRepository, RolePermissionRepository>();
        services.AddScoped<IFavoriteListRepository, FavoriteListRepository>();
        services.AddScoped<IPlaceReviewRepository, PlaceReviewRepository>();
        services.AddScoped<IPlaceSearchQueryRepository, PlaceSearchQueryRepository>();
        services.AddScoped<IGeographicCatalogRepository, GeographicCatalogRepository>();
        services.AddScoped<IRoleCatalogRepository, RoleCatalogRepository>();

        return services;
    }
}
