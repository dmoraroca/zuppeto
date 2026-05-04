using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Zuppeto.Infrastructure.Persistence.Entities;

namespace Zuppeto.Infrastructure.Persistence;

/// <summary>
/// Inserts a small demo catalog when the database has no places (development UX).
/// </summary>
public sealed class DevelopmentPlacesSeeder(ZuppetoDbContext dbContext, IHostEnvironment environment)
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (!environment.IsDevelopment())
        {
            return;
        }

        if (await dbContext.Places.AnyAsync(cancellationToken))
        {
            return;
        }

        dbContext.Places.AddRange(CreateDemoPlaces());
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static IEnumerable<PlaceRecord> CreateDemoPlaces()
    {
        yield return new PlaceRecord
        {
            Id = Guid.Parse("a1000001-0001-4001-8001-000000000001"),
            Name = "Brisa Bistro",
            Type = "Restaurant",
            ShortDescription = "Terrassa urbana amb menú casual i tracte fàcil per a gossos.",
            Description =
                "Restaurant lluminós a prop del centre amb terrassa ampla, aigua per a mascotes i ambient relaxat.",
            CoverImageUrl =
                "https://images.unsplash.com/photo-1552566626-52f8b828add9?auto=format&fit=crop&w=1200&q=80",
            AddressLine1 = "Carrer de la Marina 118",
            City = "Barcelona",
            Country = "Espanya",
            Neighborhood = "Vila Olímpica",
            Latitude = 41.390205m,
            Longitude = 2.191987m,
            ExcludeFromOsmMap = false,
            AcceptsDogs = true,
            AcceptsCats = false,
            PetPolicyLabel = "Sense suplement per gossos",
            PetPolicyNotes = "Gossos benvinguts a la terrassa i a l’interior en hores tranquil·les.",
            PricingLabel = "20-35 € per persona",
            RatingAverage = 4.60m,
            ReviewCount = 187,
            DataProvenance = "Internal",
            GooglePlaceId = null,
            GoogleCoordinatesCachedUntil = null,
            LastGoogleSyncAt = null
        };

        yield return new PlaceRecord
        {
            Id = Guid.Parse("a1000001-0001-4001-8001-000000000002"),
            Name = "Pawtel Gotic",
            Type = "Hotel",
            ShortDescription = "Hotel pet-friendly per escapades urbanes i estades curtes.",
            Description =
                "Hotel boutique amb habitacions àmplies, llits per a mascotes sota petició i bona connexió amb zones de passeig.",
            CoverImageUrl =
                "https://images.unsplash.com/photo-1566073771259-6a8506099945?auto=format&fit=crop&w=1200&q=80",
            AddressLine1 = "Via Laietana 44",
            City = "Barcelona",
            Country = "Espanya",
            Neighborhood = "Barri Gòtic",
            Latitude = 41.386602m,
            Longitude = 2.177442m,
            ExcludeFromOsmMap = false,
            AcceptsDogs = true,
            AcceptsCats = true,
            PetPolicyLabel = "Suplement fix per mascota i nit",
            PetPolicyNotes = "Accepta gossos i gats petits. Suplement fix per nit.",
            PricingLabel = "145-190 € per nit",
            RatingAverage = 4.80m,
            ReviewCount = 312,
            DataProvenance = "Internal",
            GooglePlaceId = null,
            GoogleCoordinatesCachedUntil = null,
            LastGoogleSyncAt = null
        };

        yield return new PlaceRecord
        {
            Id = Guid.Parse("a1000001-0001-4001-8001-000000000003"),
            Name = "Latido Park",
            Type = "Park",
            ShortDescription = "Espai verd per passejar, jugar i descansar al mig de la ciutat.",
            Description =
                "Parc obert amb ombra, zones de descans i accés fàcil des de barris residencials.",
            CoverImageUrl =
                "https://images.unsplash.com/photo-1511497584788-876760111969?auto=format&fit=crop&w=1200&q=80",
            AddressLine1 = "Paseo del Prado 48",
            City = "Madrid",
            Country = "Espanya",
            Neighborhood = "Paisaje del Arte",
            Latitude = 40.416775m,
            Longitude = -3.703790m,
            ExcludeFromOsmMap = false,
            AcceptsDogs = true,
            AcceptsCats = false,
            PetPolicyLabel = "Gossos lligats en zones concorregudes",
            PetPolicyNotes = "Bossa d’aigua recomanada a l’estiu.",
            PricingLabel = "Gratuït",
            RatingAverage = 4.40m,
            ReviewCount = 96,
            DataProvenance = "Internal",
            GooglePlaceId = null,
            GoogleCoordinatesCachedUntil = null,
            LastGoogleSyncAt = null
        };
    }
}
