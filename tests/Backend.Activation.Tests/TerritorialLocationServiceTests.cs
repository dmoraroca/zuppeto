using Zuppeto.Application.TerritorialImports;
using Xunit;

namespace Backend.Activation.Tests;

public sealed class TerritorialLocationServiceTests
{
    [Fact]
    public async Task Valid_selection_accepts_unicode_and_homonym_context()
    {
        var country = Guid.NewGuid();
        var locality = Guid.NewGuid();
        var repository = new FakeRepository(country, locality);
        var service = new TerritorialLocationService(repository);

        await service.ValidateSelectionAsync(new(country, locality));
        var page = await service.SearchLocalitiesAsync(country, "Mün", 1, 20);

        Assert.Equal("München", Assert.Single(page.Items).Name);
        Assert.Contains("Bayern", page.Items.Single().Context);
    }

    [Fact]
    public async Task Empty_country_or_locality_is_rejected_before_persistence()
    {
        var service = new TerritorialLocationService(new FakeRepository(Guid.NewGuid(), Guid.NewGuid()));
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ValidateSelectionAsync(new(Guid.Empty, Guid.NewGuid())));
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ValidateSelectionAsync(new(Guid.NewGuid(), Guid.Empty)));
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.SearchLocalitiesAsync(Guid.Empty, null, 1, 20));
    }

    [Theory]
    [InlineData(Failure.CountryMissing)]
    [InlineData(Failure.LocalityMissing)]
    [InlineData(Failure.OtherCountry)]
    [InlineData(Failure.Inactive)]
    [InlineData(Failure.NotSelectable)]
    public async Task Repository_rejections_are_never_hidden(Failure failure)
    {
        var country = Guid.NewGuid();
        var locality = Guid.NewGuid();
        var service = new TerritorialLocationService(new FakeRepository(country, locality, failure));
        await Assert.ThrowsAnyAsync<Exception>(() => service.ValidateSelectionAsync(new(country, locality)));
    }

    public enum Failure { None, CountryMissing, LocalityMissing, OtherCountry, Inactive, NotSelectable }

    private sealed class FakeRepository(Guid country, Guid locality, Failure failure = Failure.None) : ITerritorialLocationRepository
    {
        public Task<IReadOnlyCollection<TerritorialAdminCountryDto>> ListCountriesAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyCollection<TerritorialAdminCountryDto>>([new(country, "DE", "Deutschland", "DE", "DEU", true)]);

        public Task<PageResult<TerritorialLocalityOptionDto>> SearchLocalitiesAsync(Guid countryId, string? search, int page, int pageSize, CancellationToken cancellationToken = default) =>
            Task.FromResult(new PageResult<TerritorialLocalityOptionDto>(
                [new(locality, country, "München", "Bayern · Deutschland", "de-DE")], page, pageSize, 1));

        public Task ValidateSelectionAsync(Guid countryId, Guid territorialUnitId, CancellationToken cancellationToken = default) =>
            failure switch
            {
                Failure.CountryMissing or Failure.LocalityMissing => throw new KeyNotFoundException(),
                Failure.OtherCountry => throw new InvalidOperationException("La localitat no pertany al país indicat."),
                Failure.Inactive => throw new InvalidOperationException("La localitat està inactiva."),
                Failure.NotSelectable => throw new InvalidOperationException("La unitat territorial no és una localitat seleccionable."),
                _ => Task.CompletedTask
            };
    }
}
