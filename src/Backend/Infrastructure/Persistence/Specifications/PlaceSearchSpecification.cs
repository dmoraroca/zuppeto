using Microsoft.EntityFrameworkCore;
using Zuppeto.Domain.Places;
using Zuppeto.Infrastructure.Persistence.Entities;

namespace Zuppeto.Infrastructure.Persistence.Specifications;

internal sealed class PlaceSearchSpecification(PlaceSearchCriteria criteria) : IQuerySpecification<PlaceRecord>
{
    public IQueryable<PlaceRecord> Apply(IQueryable<PlaceRecord> query)
    {
        if (!string.IsNullOrWhiteSpace(criteria.City))
        {
            query = query.Where(place => place.City == criteria.City);
        }

        if (criteria.Type is not null)
        {
            var typeValue = criteria.Type.Value.ToString();
            query = query.Where(place => place.Type == typeValue);
        }

        query = criteria.PetCategory switch
        {
            PetCategory.Dogs => query.Where(place => place.AcceptsDogs),
            PetCategory.Cats => query.Where(place => place.AcceptsCats),
            _ => query
        };

        if (!string.IsNullOrWhiteSpace(criteria.SearchText))
        {
            var searchPattern = $"%{criteria.SearchText}%";
            query = query.Where(place =>
                EF.Functions.ILike(place.Name, searchPattern) ||
                EF.Functions.ILike(place.ShortDescription, searchPattern) ||
                EF.Functions.ILike(place.Description, searchPattern) ||
                EF.Functions.ILike(place.City, searchPattern) ||
                EF.Functions.ILike(place.Country, searchPattern) ||
                EF.Functions.ILike(place.Neighborhood, searchPattern) ||
                EF.Functions.ILike(place.AddressLine1, searchPattern));
        }

        return query;
    }
}
