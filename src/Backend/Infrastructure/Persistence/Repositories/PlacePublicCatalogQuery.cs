using Microsoft.EntityFrameworkCore;
using Zuppeto.Infrastructure.Persistence.Entities;

namespace Zuppeto.Infrastructure.Persistence.Repositories;

internal static class PlacePublicCatalogQuery
{
    internal static IQueryable<PlaceRecord> ApplyPublicVisibility(
        this IQueryable<PlaceRecord> places,
        ZuppetoDbContext dbContext) =>
        places.Where(place =>
            place.CommercialCategory != "service" &&
            place.CommercialCategory != "fuel" &&
            !dbContext.PlaceSourceRecords.Any(source =>
                source.CanonicalPlaceId == place.Id &&
                ((source.VerificationStatus == "Rejected" && source.State == "Excluded") ||
                 EF.Functions.JsonContains(source.NormalizedPayloadJson, "{\"category.original\":\"water_utility_provider\"}") ||
                 EF.Functions.JsonContains(source.NormalizedPayloadJson, "{\"category.subcategory\":\"water_utility_provider\"}") ||
                 EF.Functions.JsonContains(source.NormalizedPayloadJson, "{\"category.original\":\"public_utility\"}") ||
                 EF.Functions.JsonContains(source.NormalizedPayloadJson, "{\"category.subcategory\":\"public_utility\"}") ||
                 EF.Functions.JsonContains(source.NormalizedPayloadJson, "{\"category.original\":\"electric_utility_provider\"}") ||
                 EF.Functions.JsonContains(source.NormalizedPayloadJson, "{\"category.subcategory\":\"electric_utility_provider\"}") ||
                 EF.Functions.JsonContains(source.NormalizedPayloadJson, "{\"category.original\":\"b2b_water_treatment_service\"}") ||
                 EF.Functions.JsonContains(source.NormalizedPayloadJson, "{\"category.subcategory\":\"b2b_water_treatment_service\"}") ||
                 EF.Functions.JsonContains(source.NormalizedPayloadJson, "{\"category.original\":\"garbage_collection_service\"}") ||
                 EF.Functions.JsonContains(source.NormalizedPayloadJson, "{\"category.subcategory\":\"garbage_collection_service\"}") ||
                 EF.Functions.JsonContains(source.NormalizedPayloadJson, "{\"category.original\":\"recycling_center\"}") ||
                 EF.Functions.JsonContains(source.NormalizedPayloadJson, "{\"category.subcategory\":\"recycling_center\"}") ||
                 EF.Functions.JsonContains(source.NormalizedPayloadJson, "{\"category.original\":\"gas_station\"}") ||
                 EF.Functions.JsonContains(source.NormalizedPayloadJson, "{\"category.subcategory\":\"gas_station\"}") ||
                 EF.Functions.JsonContains(source.NormalizedPayloadJson, "{\"category.original\":\"petrol_station\"}") ||
                 EF.Functions.JsonContains(source.NormalizedPayloadJson, "{\"category.subcategory\":\"petrol_station\"}") ||
                 EF.Functions.JsonContains(source.NormalizedPayloadJson, "{\"category.original\":\"electric_vehicle_charging_station\"}") ||
                 EF.Functions.JsonContains(source.NormalizedPayloadJson, "{\"category.subcategory\":\"electric_vehicle_charging_station\"}") ||
                 EF.Functions.JsonContains(source.NormalizedPayloadJson, "{\"category.original\":\"parking\"}") ||
                 EF.Functions.JsonContains(source.NormalizedPayloadJson, "{\"category.subcategory\":\"parking\"}") ||
                 EF.Functions.JsonContains(source.NormalizedPayloadJson, "{\"category.original\":\"parking_lot\"}") ||
                 EF.Functions.JsonContains(source.NormalizedPayloadJson, "{\"category.subcategory\":\"parking_lot\"}"))));
}
