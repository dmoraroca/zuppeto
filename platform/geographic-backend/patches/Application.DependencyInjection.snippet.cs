// Afegir als `using` (prop de `namespace`):
//   using YepPet.Application.Admin;
//   using YepPet.Application.Admin.Validators;

// Dins de AddApplication(), amb la resta de `AddScoped` / `AddSingleton`:
        services.AddScoped<IGeographicAdminAppService, GeographicAdminAppService>();
        services.AddSingleton<IValidator<CreateCountryRequest>, CreateCountryRequestValidator>();
        services.AddSingleton<IValidator<UpdateCountryRequest>, UpdateCountryRequestValidator>();
        services.AddSingleton<IValidator<CreateCityRequest>, CreateCityRequestValidator>();
        services.AddSingleton<IValidator<UpdateCityRequest>, UpdateCityRequestValidator>();
