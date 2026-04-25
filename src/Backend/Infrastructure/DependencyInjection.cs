using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using YepPet.Application.Auth;
using YepPet.Application.Places;
using YepPet.Domain.Abstractions;
using YepPet.Infrastructure.Auth;
using YepPet.Infrastructure.GeoNames;
using YepPet.Infrastructure.Persistence;
using YepPet.Infrastructure.Persistence.Repositories;
using YepPet.Infrastructure.RabbitMq;

namespace YepPet.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment hostEnvironment)
    {
        var connectionString = configuration.GetConnectionString("YepPet")
            ?? "Host=localhost;Port=5433;Database=yeppet;Username=app;Password=app";
        var expiresInMinutes = int.TryParse(configuration["Auth:Jwt:ExpiresInMinutes"], out var parsedExpiresInMinutes)
            ? parsedExpiresInMinutes
            : 480;
        var authOptions = new AuthOptions
        {
            FrontendBaseUrl = configuration["Auth:FrontendBaseUrl"] ?? "http://localhost:4200",
            Jwt = new AuthOptions.JwtOptions
            {
                Issuer = configuration["Auth:Jwt:Issuer"] ?? "YepPet",
                Audience = configuration["Auth:Jwt:Audience"] ?? "YepPet.Web",
                SigningKey = configuration["Auth:Jwt:SigningKey"]
                    ?? "dev-only-yep-pet-signing-key-change-in-production-123456789",
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
        services.AddDbContext<YepPetDbContext>((sp, options) =>
        {
            options.UseNpgsql(connectionString);
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
        services.AddScoped<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddScoped<IAccessTokenIssuer, JwtAccessTokenIssuer>();
        services.AddScoped<IGoogleIdTokenVerifier, GoogleIdTokenVerifier>();
        services.AddScoped<DevelopmentIdentitySeeder>();
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
