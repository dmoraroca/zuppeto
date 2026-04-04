namespace YepPet.Infrastructure.Persistence.Specifications;

internal interface IQuerySpecification<T>
{
    IQueryable<T> Apply(IQueryable<T> query);
}
