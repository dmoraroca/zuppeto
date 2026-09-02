using Microsoft.Extensions.DependencyInjection;
using Zuppeto.Application.Admin;
using Zuppeto.Application.Auth;
using Zuppeto.Application.Events;
using Zuppeto.Application.Factories;
using Zuppeto.Application.Favorites;
using Zuppeto.Application.Navigation;
using Zuppeto.Application.Places;
using Zuppeto.Application.Reviews;
using Zuppeto.Application.Users;
using Zuppeto.Application.Validation;
using Zuppeto.Application.Auth.Validators;
using Zuppeto.Application.Admin.Validators;
using Zuppeto.Application.Admin.Events;
using Zuppeto.Application.Admin.Commands;
using Zuppeto.Application.Commands;
using Zuppeto.Application.Users.Validators;
using Zuppeto.Application.Places.Validators;

namespace Zuppeto.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAuthApplicationService, AuthApplicationService>();
        services.AddScoped<IAdminApplicationService, AdminApplicationService>();
        services.AddScoped<IGeographicAdminAppService, GeographicAdminAppService>();
        services.AddScoped<INavigationApplicationService, NavigationApplicationService>();
        services.AddScoped<PlaceCoverPhotoStore>();
        services.AddScoped<PlaceResponseMapper>();
        services.AddScoped<PlaceSearchPageAssembler>();
        services.AddScoped<PlaceGoogleSearchIngest>();
        services.AddScoped<PlaceGoogleDetailsEnricher>();
        services.AddScoped<IPlaceApplicationService, PlaceApplicationService>();
        services.AddScoped<IFavoriteListApplicationService, FavoriteListApplicationService>();
        services.AddScoped<IUserApplicationService, UserApplicationService>();
        services.AddScoped<IPlaceReviewApplicationService, PlaceReviewApplicationService>();

        services.AddSingleton<IEventPublisher, InMemoryEventPublisher>();
        services.AddSingleton<IEventHandler<UserCreatedEvent>, AuditUserEventsHandler>();
        services.AddSingleton<IEventHandler<UserRoleChangedEvent>, AuditUserEventsHandler>();
        services.AddSingleton<IUserProfileFactory, UserProfileFactory>();
        services.AddSingleton<IMenuItemDefinitionFactory, MenuItemDefinitionFactory>();
        services.AddScoped<ICommandHandler<CreateAdminUserCommand, Results.Result<Users.UserDto>>, CreateAdminUserCommandHandler>();
        services.AddScoped<ICommandHandler<UpdateUserRoleCommand, Results.Result<Users.UserDto>>, UpdateUserRoleCommandHandler>();

        services.AddSingleton<IValidator<LoginRequest>, LoginRequestValidator>();
        services.AddSingleton<IValidator<GoogleLoginRequest>, GoogleLoginRequestValidator>();
        services.AddSingleton<IValidator<CreateAdminUserRequest>, CreateAdminUserRequestValidator>();
        services.AddSingleton<IValidator<UpdateUserRoleRequest>, UpdateUserRoleRequestValidator>();
        services.AddSingleton<IValidator<UpdateRolePermissionsRequest>, UpdateRolePermissionsRequestValidator>();
        services.AddSingleton<IValidator<SaveMenuRequest>, SaveMenuRequestValidator>();
        services.AddSingleton<IValidator<CreateCountryRequest>, CreateCountryRequestValidator>();
        services.AddSingleton<IValidator<UpdateCountryRequest>, UpdateCountryRequestValidator>();
        services.AddSingleton<IValidator<CreateCityRequest>, CreateCityRequestValidator>();
        services.AddSingleton<IValidator<UpdateCityRequest>, UpdateCityRequestValidator>();
        services.AddSingleton<IValidator<CreatePermissionDefinitionRequest>, CreatePermissionDefinitionRequestValidator>();
        services.AddSingleton<IValidator<UpdatePermissionDefinitionRequest>, UpdatePermissionDefinitionRequestValidator>();
        services.AddSingleton<IValidator<CreateRoleDefinitionRequest>, CreateRoleDefinitionRequestValidator>();
        services.AddSingleton<IValidator<UpdateRoleDefinitionRequest>, UpdateRoleDefinitionRequestValidator>();
        services.AddSingleton<IValidator<UserRegistrationRequest>, UserRegistrationRequestValidator>();
        services.AddSingleton<IValidator<UserProfileUpdateRequest>, UserProfileUpdateRequestValidator>();
        services.AddSingleton<IValidator<UserAccountUpdateRequest>, UserAccountUpdateRequestValidator>();
        services.AddSingleton<IValidator<PlaceUpsertRequest>, PlaceUpsertRequestValidator>();
        services.AddSingleton<IValidator<PlaceCitySearchRequest>, PlaceCitySearchRequestValidator>();

        return services;
    }
}
