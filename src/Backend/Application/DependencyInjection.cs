using Microsoft.Extensions.DependencyInjection;
using YepPet.Application.Admin;
using YepPet.Application.Auth;
using YepPet.Application.Events;
using YepPet.Application.Factories;
using YepPet.Application.Favorites;
using YepPet.Application.Navigation;
using YepPet.Application.Places;
using YepPet.Application.Reviews;
using YepPet.Application.Users;
using YepPet.Application.Validation;
using YepPet.Application.Auth.Validators;
using YepPet.Application.Admin.Validators;
using YepPet.Application.Admin.Events;
using YepPet.Application.Admin.Commands;
using YepPet.Application.Commands;
using YepPet.Application.Users.Validators;
using YepPet.Application.Places.Validators;

namespace YepPet.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAuthApplicationService, AuthApplicationService>();
        services.AddScoped<IAdminApplicationService, AdminApplicationService>();
        services.AddScoped<IGeographicAdminAppService, GeographicAdminAppService>();
        services.AddScoped<INavigationApplicationService, NavigationApplicationService>();
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
        services.AddSingleton<IValidator<PlaceUpsertRequest>, PlaceUpsertRequestValidator>();

        return services;
    }
}
