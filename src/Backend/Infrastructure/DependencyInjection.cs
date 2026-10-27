using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Zuppeto.Application.Auth;
using Zuppeto.Application.Users;
using Zuppeto.Application.Places;
using Zuppeto.Domain.Abstractions;
using Zuppeto.Infrastructure.Auth;
using Zuppeto.Infrastructure.Places.Wikidata;
using Zuppeto.Infrastructure.Persistence;
using Zuppeto.Infrastructure.Persistence.Repositories;
using Zuppeto.Application.TerritorialImports;
using Zuppeto.Infrastructure.TerritorialImports;
using Zuppeto.Infrastructure.Places;
using Zuppeto.Infrastructure.RabbitMq;
using Zuppeto.Infrastructure.Email;
using Zuppeto.Infrastructure.Places.OpenStreetMap;
using Zuppeto.Infrastructure.Places.Mapillary;
using Zuppeto.Infrastructure.Places.MunicipalOpenData;
using Zuppeto.Infrastructure.Places.OvertureMaps;
using Zuppeto.Infrastructure.Places.PublicAdministration;
using Zuppeto.Infrastructure.Perplexity;
using Zuppeto.Application.ExternalIntelligence;

namespace Zuppeto.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment hostEnvironment)
    {
        var connectionString = configuration.GetConnectionString("Zuppeto");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Falta ConnectionStrings:Zuppeto. Configura la connexió fora de Git.");
        }

        var authOptions = AuthOptionsFactory.Create(configuration);

        services.AddSingleton(Options.Create(authOptions));
        services.Configure<DevelopmentIdentitySeedOptions>(
            configuration.GetSection(DevelopmentIdentitySeedOptions.SectionName));
        services.AddRabbitMq(configuration);
        services.Configure<AccountActivationOptions>(configuration.GetSection(AccountActivationOptions.SectionName));
        services.AddSingleton<DevelopmentActivationInbox>();
        services.AddSingleton<DevelopmentPasswordRecoveryInbox>();
        var activationDeliveryMode = configuration["AccountActivation:DeliveryMode"] ?? "DevelopmentInbox";
        if (string.Equals(activationDeliveryMode, "Smtp", StringComparison.OrdinalIgnoreCase))
        {
            services.AddScoped<IAccountActivationEmailSender, SmtpAccountActivationEmailSender>();
            services.AddScoped<IPasswordRecoveryEmailSender, SmtpPasswordRecoveryEmailSender>();
        }
        else
        {
            services.AddScoped<IAccountActivationEmailSender, DevelopmentInboxAccountActivationEmailSender>();
            services.AddScoped<IPasswordRecoveryEmailSender, DevelopmentInboxPasswordRecoveryEmailSender>();
        }
        services.AddMemoryCache();
        services.AddDataProtection();
        services.Configure<OverpassOptions>(configuration.GetSection(OverpassOptions.SectionName));
        services.Configure<GeofabrikExtractOptions>(configuration.GetSection(GeofabrikExtractOptions.SectionName));
        services.Configure<OsmDirectMapOptions>(configuration.GetSection(OsmDirectMapOptions.SectionName));
        services.Configure<WikidataOptions>(configuration.GetSection(WikidataOptions.SectionName));
        services.Configure<DibaMunicipalEstablishmentsOptions>(
            configuration.GetSection(DibaMunicipalEstablishmentsOptions.SectionName));
        services.Configure<OvertureMapsOptions>(configuration.GetSection(OvertureMapsOptions.SectionName));
        services.Configure<CataloniaTourismRegistryOptions>(
            configuration.GetSection(CataloniaTourismRegistryOptions.SectionName));
        services.Configure<AragonCommercialRegistryOptions>(
            configuration.GetSection(AragonCommercialRegistryOptions.SectionName));
        services.AddSingleton<IValidateOptions<MapillaryOptions>, MapillaryOptionsValidator>();
        services.AddOptions<MapillaryOptions>()
            .Bind(configuration.GetSection(MapillaryOptions.SectionName))
            .ValidateOnStart();
        services.AddSingleton<IConfigureOptions<PerplexityOptions>, PerplexityOptionsSetup>();
        services.AddSingleton<IValidateOptions<PerplexityOptions>, PerplexityOptionsValidator>();
        services.AddOptions<PerplexityOptions>()
            .ValidateOnStart();
        services.AddHttpClient<IExternalIntelligenceProvider, PerplexityExternalIntelligenceProvider>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<PerplexityOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(Math.Clamp(options.TimeoutSeconds, 5, 120));
            client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "PetiLoc-External-Intelligence/1.0");
        }).RedactLoggedHeaders(["Authorization"]);
        services.AddDbContext<ZuppetoDbContext>((sp, options) =>
        {
            options.UseNpgsql(connectionString);
            options.AddInterceptors(new ClampStringMaxLengthInterceptor());
            if (hostEnvironment.IsDevelopment())
            {
                options.EnableDetailedErrors();
                if (configuration.GetValue("Zuppeto:EnableSensitiveDataLogging", false))
                {
                    options.EnableSensitiveDataLogging();
                }
            }
        });
        // Facebook encara exigeix credencials al query string en aquests endpoints.
        // Desactivem els loggers HTTP per impedir que la URL sensible arribi als logs.
        services.AddHttpClient<IFacebookOAuthClient, FacebookOAuthClient>()
            .RemoveAllLoggers();
        services.AddHttpClient<IPlaceWebsitePageReader, HttpPlaceWebsitePageReader>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(8);
            client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "ZuppetoPlaceEnrichment/1.0");
        });
        services.AddHttpClient<OverpassPlaceDiscoverySource>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<OverpassOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl);
            client.Timeout = Timeout.InfiniteTimeSpan;
            client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "PetiLoc-OSM-Pilot/1.0 (admin import; https://github.com/dmoraroca/zuppeto)");
        });
        services.AddScoped<IPlaceDiscoverySource, OverpassPolicyDiscoveryDecorator>();
        services.AddScoped<IPlaceDiscoverySource, GeofabrikPbfPlaceDiscoverySource>();
        services.AddHttpClient<IPlaceDiscoverySource, OsmDirectMapPlaceDiscoverySource>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<OsmDirectMapOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(Math.Clamp(options.TimeoutSeconds, 5, 60));
            client.DefaultRequestHeaders.TryAddWithoutValidation(
                "User-Agent",
                "PetiLoc-OSM-Direct-Pilot/1.0 (admin audit; https://github.com/dmoraroca/zuppeto)");
        });
        services.AddHttpClient<IPlaceDiscoverySource, DibaMunicipalEstablishmentsSource>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<DibaMunicipalEstablishmentsOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(Math.Clamp(options.TimeoutSeconds, 5, 60));
            client.DefaultRequestHeaders.TryAddWithoutValidation(
                "User-Agent",
                "PetiLoc-DIBA-Municipal-Audit/1.0 (admin import; https://github.com/dmoraroca/zuppeto)");
        });
        services.AddHttpClient<IPlaceDiscoverySource, CataloniaTourismRegistrySource>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<CataloniaTourismRegistryOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(Math.Clamp(options.TimeoutSeconds, 5, 60));
            client.DefaultRequestHeaders.TryAddWithoutValidation(
                "User-Agent",
                "PetiLoc-Catalonia-Tourism-Audit/1.0 (admin import; https://github.com/dmoraroca/zuppeto)");
        });
        services.AddHttpClient<IPlaceDiscoverySource, AragonCommercialRegistrySource>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<AragonCommercialRegistryOptions>>().Value;
            client.Timeout = TimeSpan.FromSeconds(Math.Clamp(options.TimeoutSeconds, 30, 180));
            client.DefaultRequestHeaders.TryAddWithoutValidation(
                "User-Agent",
                "PetiLoc-Aragon-Commerce-Audit/1.0 (admin import; https://github.com/dmoraroca/zuppeto)");
        });
        services.AddScoped<IPlaceDiscoverySource, OvertureMapsPlaceDiscoverySource>();
        services.AddHttpClient<IPlaceEnrichmentSource, WikidataEnrichmentSource>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<WikidataOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(Math.Clamp(options.TimeoutSeconds, 5, 60));
            client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "PetiLoc-Wikidata-Enrichment/1.0 (https://github.com/dmoraroca/zuppeto)");
            client.DefaultRequestHeaders.TryAddWithoutValidation("Accept-Encoding", "gzip, deflate");
        }).ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
        {
            AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate
        });
        services.AddHttpClient("WikidataMedia", (sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<WikidataOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(Math.Clamp(options.TimeoutSeconds, 5, 60));
            client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "PetiLoc-Media/1.0 (https://github.com/dmoraroca/zuppeto)");
        });
        services.AddHttpClient("CommonsMedia", (sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<WikidataOptions>>().Value;
            client.BaseAddress = new Uri(options.CommonsApiBaseUrl);
            client.Timeout = TimeSpan.FromSeconds(Math.Clamp(options.TimeoutSeconds, 5, 60));
            client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "PetiLoc-Media/1.0 (https://github.com/dmoraroca/zuppeto)");
        });
        services.AddScoped<IPlaceMediaSource, WikimediaCommonsMediaSource>();
        services.AddScoped<IPlaceMediaProcessor, SkiaSharpPlaceMediaProcessor>();
        services.AddScoped<IMissingPlaceProposalRepository, MissingPlaceProposalRepository>();
        services.AddHttpClient<IPlaceProximityMediaSource, MapillaryMediaSource>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<MapillaryOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(Math.Clamp(options.TimeoutSeconds, 5, 60));
            client.DefaultRequestHeaders.TryAddWithoutValidation(
                "User-Agent",
                "PetiLoc-Mapillary-Admin/1.0 (https://github.com/dmoraroca/zuppeto)");
        }).RedactLoggedHeaders(["Authorization"]);
        services.AddScoped<OsmPlaceSourceNormalizer>();
        services.AddScoped<DibaMunicipalPlaceSourceNormalizer>();
        services.AddScoped<PublicAdministrationPlaceSourceNormalizer>();
        services.AddScoped<OverturePlaceSourceNormalizer>();
        services.AddScoped<IPlaceSourceNormalizer, CompositePlaceSourceNormalizer>();
        services.AddScoped<OsmStoragePermissionPolicy>();
        services.AddScoped<DibaMunicipalStoragePermissionPolicy>();
        services.AddScoped<PublicAdministrationStoragePermissionPolicy>();
        services.AddScoped<OvertureStoragePermissionPolicy>();
        services.AddScoped<IPlaceStoragePermissionPolicy, CompositePlaceStoragePermissionPolicy>();
        services.AddScoped<IPlaceMatcher, ConservativePlaceMatcher>();
        services.AddScoped<IPlaceImportRepository, PlaceImportRepository>();
        services.AddScoped<IPlaceCatalogGovernanceRepository, PlaceCatalogGovernanceRepository>();
        services.AddScoped<IPlaceImportWorkQueue, PlaceImportWorkQueue>();
        services.AddHostedService<PlaceImportWorker>();
        services.AddSingleton<IPlaceCoverStorage, FilePlaceCoverStorage>();
        services.AddSingleton<IPlaceMediaBinaryStorage, FilePlaceMediaBinaryStorage>();
        services.AddScoped<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddScoped<IAccessTokenIssuer, JwtAccessTokenIssuer>();
        services.AddScoped<IAccessTokenRevocationStore, AccessTokenRevocationStore>();
        services.AddSingleton<ITotpService, TotpService>();
        services.AddSingleton<ITwoFactorChallengeStore, MemoryTwoFactorChallengeStore>();
        services.AddScoped<IGoogleIdTokenVerifier, GoogleIdTokenVerifier>();
        services.AddScoped<DevelopmentIdentitySeeder>();
        services.AddScoped<DevelopmentPlacesSeeder>();
        services.AddScoped<TerritorialPilotConfigurationSeeder>();
        services.AddScoped<IMenuRepository, MenuRepository>();
        services.AddScoped<IPlaceRepository, PlaceRepository>();
        services.AddScoped<IPlaceMediaRepository, PlaceMediaRepository>();
        services.AddScoped<IPlacePublicSearchQuery, PlacePublicSearchQuery>();
        services.AddScoped<IPlacePublicVisibilityQuery, PlacePublicVisibilityQuery>();
        services.AddScoped<IPlaceDataAttributionQuery, PlaceDataAttributionQuery>();
        services.AddScoped<IPetGovernanceRepository, PetGovernanceRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IExternalIdentityRepository, ExternalIdentityRepository>();
        services.AddScoped<ITotpRecoveryCodeRepository, TotpRecoveryCodeRepository>();
        services.AddScoped<IRolePermissionRepository, RolePermissionRepository>();
        services.AddScoped<IFavoriteListRepository, FavoriteListRepository>();
        services.AddScoped<IPlaceReviewRepository, PlaceReviewRepository>();
        services.AddScoped<IPlaceSearchQueryRepository, PlaceSearchQueryRepository>();
        services.AddScoped<IExternalIntelligenceStore, ExternalIntelligenceStore>();
        services.AddScoped<IGeographicCatalogRepository, GeographicCatalogRepository>();
        services.AddScoped<IRoleCatalogRepository, RoleCatalogRepository>();
        services.AddSingleton<ITerritorialWorkbookReader, XlsxTerritorialReader>();
        services.AddScoped<ITerritorialImportAuthorizer, TerritorialImportAuthorizer>();
        services.AddScoped<ITerritorialImportStore, TerritorialImportStore>();
        services.AddScoped<ITerritorialImportWorkQueue, TerritorialImportWorkQueue>();
        services.AddScoped<ITerritorialCatalogImportGateway, TerritorialCatalogImportGateway>();
        services.AddScoped<ITerritorialAdminRepository, TerritorialAdminRepository>();
        services.AddScoped<ITerritorialLocationRepository, TerritorialLocationRepository>();
        services.AddScoped<ITerritorialContractSourceResolver, TerritorialContractSourceResolver>();
        services.AddHostedService<TerritorialImportWorker>();

        return services;
    }
}
