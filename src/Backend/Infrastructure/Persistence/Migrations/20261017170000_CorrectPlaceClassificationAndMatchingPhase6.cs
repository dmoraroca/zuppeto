using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Zuppeto.Infrastructure.Persistence;

#nullable disable

namespace Infrastructure.Persistence.Migrations;

[DbContext(typeof(ZuppetoDbContext))]
[Migration("20261017170000_CorrectPlaceClassificationAndMatchingPhase6")]
public partial class CorrectPlaceClassificationAndMatchingPhase6 : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            UPDATE places
            SET type = CASE
                WHEN commercial_category IN ('restaurant', 'cafe', 'bar', 'pub', 'fast_food', 'ice_cream', 'bakery', 'biergarten', 'food_court') THEN 'FoodAndDrink'
                WHEN commercial_category IN ('hotel', 'hostel', 'motel', 'apartment', 'camping') THEN 'Accommodation'
                WHEN commercial_category IN ('shop', 'pet_shop') THEN 'Retail'
                WHEN commercial_category = 'pharmacy' THEN 'Health'
                WHEN commercial_category = 'fuel' THEN 'Mobility'
                WHEN commercial_category IN ('park', 'dog_park') THEN 'Outdoor'
                WHEN commercial_category IN ('veterinary', 'pet_service', 'pet_grooming') THEN 'PetCare'
                WHEN commercial_category = 'nightclub' THEN 'Entertainment'
                ELSE type
            END
            WHERE commercial_category IS NOT NULL;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            UPDATE places
            SET type = CASE
                WHEN commercial_category = 'restaurant' THEN 'Restaurant'
                WHEN commercial_category IN ('cafe', 'bar', 'pub', 'fast_food', 'ice_cream', 'bakery') THEN 'Bar'
                WHEN commercial_category IN ('hotel', 'hostel') THEN 'Hotel'
                WHEN commercial_category = 'apartment' THEN 'Apartment'
                WHEN commercial_category IN ('park', 'dog_park') THEN 'Park'
                ELSE 'Service'
            END
            WHERE commercial_category IS NOT NULL;
            """);
    }
}
