#!/usr/bin/env bash
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
SRC="$ROOT/platform/geographic-backend/mirror"
DEST="$ROOT/src/Backend"
if [[ ! -d "$SRC" ]]; then
  echo "No trobo $SRC" >&2
  exit 1
fi

mkdir -p "$DEST/Domain/Geography" "$DEST/Domain/Abstractions"
mkdir -p "$DEST/Application/Admin/Validators"
mkdir -p "$DEST/Infrastructure/Persistence/Entities" "$DEST/Infrastructure/Persistence/Configurations" "$DEST/Infrastructure/Persistence/Repositories"
mkdir -p "$DEST/Api/Endpoints"

cp -v "$SRC/Domain/Geography/CountryRow.cs" "$DEST/Domain/Geography/CountryRow.cs"
cp -v "$SRC/Domain/Geography/CityRow.cs" "$DEST/Domain/Geography/CityRow.cs"
cp -v "$SRC/Domain/Abstractions/IGeographicCatalogRepository.cs" "$DEST/Domain/Abstractions/IGeographicCatalogRepository.cs"
cp -v "$SRC/Application/Admin/GeographicAdminDtos.cs" "$DEST/Application/Admin/GeographicAdminDtos.cs"
cp -v "$SRC/Application/Admin/IGeographicAdminAppService.cs" "$DEST/Application/Admin/IGeographicAdminAppService.cs"
cp -v "$SRC/Application/Admin/GeographicAdminAppService.cs" "$DEST/Application/Admin/GeographicAdminAppService.cs"
cp -v "$SRC/Application/Admin/Validators/CreateCountryRequestValidator.cs" "$DEST/Application/Admin/Validators/CreateCountryRequestValidator.cs"
cp -v "$SRC/Application/Admin/Validators/UpdateCountryRequestValidator.cs" "$DEST/Application/Admin/Validators/UpdateCountryRequestValidator.cs"
cp -v "$SRC/Application/Admin/Validators/CreateCityRequestValidator.cs" "$DEST/Application/Admin/Validators/CreateCityRequestValidator.cs"
cp -v "$SRC/Application/Admin/Validators/UpdateCityRequestValidator.cs" "$DEST/Application/Admin/Validators/UpdateCityRequestValidator.cs"
cp -v "$SRC/Infrastructure/Persistence/Entities/CountryRecord.cs" "$DEST/Infrastructure/Persistence/Entities/CountryRecord.cs"
cp -v "$SRC/Infrastructure/Persistence/Entities/CityRecord.cs" "$DEST/Infrastructure/Persistence/Entities/CityRecord.cs"
cp -v "$SRC/Infrastructure/Persistence/Configurations/CountryConfiguration.cs" "$DEST/Infrastructure/Persistence/Configurations/CountryConfiguration.cs"
cp -v "$SRC/Infrastructure/Persistence/Configurations/CityConfiguration.cs" "$DEST/Infrastructure/Persistence/Configurations/CityConfiguration.cs"
cp -v "$SRC/Infrastructure/Persistence/Repositories/GeographicCatalogRepository.cs" "$DEST/Infrastructure/Persistence/Repositories/GeographicCatalogRepository.cs"
cp -v "$SRC/Api/Endpoints/GeographicAdminEndpoints.cs" "$DEST/Api/Endpoints/GeographicAdminEndpoints.cs"

echo ""
echo "Aplica els fragments de platform/geographic-backend/patches/ als fitxers existents:"
echo "  - Application.DependencyInjection.snippet.cs → Application/DependencyInjection.cs"
echo "  - Infrastructure.DependencyInjection.snippet.cs → Infrastructure/DependencyInjection.cs"
echo "  - YepPetDbContext.snippet.cs → Infrastructure/Persistence/YepPetDbContext.cs"
echo "  - Program.snippet.cs → Api/Program.cs"
echo "  - DevelopmentIdentitySeeder.snippet.cs → Infrastructure/Auth/DevelopmentIdentitySeeder.cs"
echo ""
echo "Després: dotnet ef migrations add AddCountriesAndCities --project src/Backend/Infrastructure --startup-project src/Backend/Api"
echo ""
