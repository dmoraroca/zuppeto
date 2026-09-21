using Zuppeto.Domain.Common;
using Zuppeto.Domain.Geography;
using Xunit;

namespace Backend.Activation.Tests;

public sealed class TerritorialDomainTests
{
    [Fact]
    public void Country_normalizes_verified_iso_codes_and_allows_transition_without_them()
    {
        var pending = new Country(Guid.NewGuid(), "Espanya");
        var verified = new Country(Guid.NewGuid(), "Alemanya", "de", "deu");

        Assert.Null(pending.Iso2);
        Assert.Null(pending.Iso3);
        Assert.Equal("DE", verified.Iso2);
        Assert.Equal("DEU", verified.Iso3);
        Assert.Throws<DomainRuleException>(() => verified.SetIsoCodes("D", "DE"));
    }

    [Fact]
    public void Territorial_type_is_country_configurable_and_selectability_is_explicit()
    {
        var type = new TerritorialUnitType(Guid.NewGuid(), Guid.NewGuid(), "municipality", "Municipi", 30, true);

        Assert.Equal("MUNICIPALITY", type.Code);
        Assert.True(type.IsSelectableLocality);
        Assert.True(type.IsActive);
    }

    [Fact]
    public void Unit_rejects_self_parent_and_hierarchy_rejects_other_country_or_cycle()
    {
        var countryId = Guid.NewGuid();
        var unitId = Guid.NewGuid();
        var unit = new TerritorialUnit(unitId, countryId, Guid.NewGuid());
        var parent = new TerritorialUnit(Guid.NewGuid(), countryId, Guid.NewGuid());
        var foreignParent = new TerritorialUnit(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        Assert.Throws<DomainRuleException>(() => unit.ChangeParent(unitId));
        Assert.Throws<DomainRuleException>(() => TerritorialHierarchy.EnsureCanAssignParent(unit, foreignParent, []));
        Assert.Throws<DomainRuleException>(() => TerritorialHierarchy.EnsureCanAssignParent(unit, parent, [unit.Id]));

        TerritorialHierarchy.EnsureCanAssignParent(unit, parent, []);
        unit.ChangeParent(parent.Id);
        Assert.Equal(parent.Id, unit.ParentId);
    }

    [Fact]
    public void Different_units_can_have_the_same_name_and_unicode_is_preserved()
    {
        var first = NewUnit();
        var second = NewUnit();
        var firstName = new TerritorialUnitName(Guid.NewGuid(), "  München  ", TerritorialNameKind.Official, "de-DE", true);
        var secondName = new TerritorialUnitName(Guid.NewGuid(), "München", TerritorialNameKind.Official, "de-DE", true);

        first.AddName(firstName);
        second.AddName(secondName);

        Assert.Equal("München", firstName.Name);
        Assert.Equal("münchen", firstName.NormalizedName);
        Assert.Single(first.Names);
        Assert.Single(second.Names);
    }

    [Fact]
    public void Unit_allows_one_primary_name_per_kind_and_locale()
    {
        var unit = NewUnit();
        unit.AddName(new TerritorialUnitName(Guid.NewGuid(), "A Coruña", TerritorialNameKind.Official, "gl-ES", true));
        unit.AddName(new TerritorialUnitName(Guid.NewGuid(), "La Coruña", TerritorialNameKind.Historic, "es-ES", true));

        Assert.Throws<DomainRuleException>(() => unit.AddName(
            new TerritorialUnitName(Guid.NewGuid(), "Coruña", TerritorialNameKind.Official, "gl-ES", true)));
    }

    [Fact]
    public void Unit_supports_multiple_namespaced_codes_and_preserves_leading_zeroes()
    {
        var unit = NewUnit();
        unit.AddCode(new TerritorialUnitCode(Guid.NewGuid(), "de:destatis:ags", "01001000", isPrimary: true));
        unit.AddCode(new TerritorialUnitCode(Guid.NewGuid(), "de:destatis:ars", "010010000000", isPrimary: true));

        Assert.Equal("01001000", unit.Codes.First().Value);
        Assert.Equal(2, unit.Codes.Count);
    }

    [Fact]
    public void Unit_rejects_overlapping_duplicate_code_but_allows_non_overlapping_reuse()
    {
        var unit = NewUnit();
        unit.AddCode(new TerritorialUnitCode(
            Guid.NewGuid(), "es:ine:municipality", "08006", new DateOnly(2020, 1, 1), new DateOnly(2024, 12, 31)));

        Assert.Throws<DomainRuleException>(() => unit.AddCode(new TerritorialUnitCode(
            Guid.NewGuid(), "es:ine:municipality", "08006", new DateOnly(2024, 1, 1))));

        unit.AddCode(new TerritorialUnitCode(
            Guid.NewGuid(), "es:ine:municipality", "08006", new DateOnly(2025, 1, 1)));
        Assert.Equal(2, unit.Codes.Count);
    }

    [Theory]
    [InlineData("es-ES")]
    [InlineData("ca-ES")]
    [InlineData("eu-ES")]
    [InlineData("gl-ES")]
    [InlineData("de-DE")]
    [InlineData("el-GR")]
    public void Locale_assignment_accepts_regional_bcp47_tags(string locale)
    {
        var assignment = new TerritorialLocaleAssignment(Guid.NewGuid(), Guid.NewGuid(), locale);
        Assert.Contains('-', assignment.Locale);
    }

    [Theory]
    [InlineData("es")]
    [InlineData("not_a_locale")]
    public void Locale_assignment_rejects_ui_or_invalid_language_codes(string locale)
    {
        Assert.Throws<DomainRuleException>(() =>
            new TerritorialLocaleAssignment(Guid.NewGuid(), Guid.NewGuid(), locale));
    }

    [Fact]
    public void Coordinates_are_optional_but_must_be_complete_in_range_and_sourced()
    {
        var unit = NewUnit();
        Assert.Null(unit.Latitude);
        Assert.Null(unit.Longitude);

        Assert.Throws<DomainRuleException>(() => unit.SetCoordinates(91, 2, Guid.NewGuid()));
        Assert.Throws<DomainRuleException>(() => unit.SetCoordinates(41, 2, Guid.Empty));

        unit.SetCoordinates(41.3851m, 2.1734m, Guid.NewGuid());
        Assert.Equal(41.3851m, unit.Latitude);
        Assert.Equal(2.1734m, unit.Longitude);
    }

    [Fact]
    public void Zero_zero_requires_explicit_source_verification()
    {
        var unit = NewUnit();
        Assert.Throws<DomainRuleException>(() => unit.SetCoordinates(0, 0, Guid.NewGuid()));

        unit.SetCoordinates(0, 0, Guid.NewGuid(), zeroZeroWasVerified: true);
        Assert.Equal(0, unit.Latitude);
        Assert.Equal(0, unit.Longitude);
    }

    [Fact]
    public void Unit_can_be_inactivated_without_losing_identity()
    {
        var unit = NewUnit();
        unit.Deactivate();
        Assert.False(unit.IsActive);
        unit.Activate();
        Assert.True(unit.IsActive);
    }

    [Fact]
    public void Dataset_source_cannot_be_approved_before_legal_terms_are_verified()
    {
        var source = new TerritorialDatasetSource(
            Guid.NewGuid(), Guid.NewGuid(), "INE", "Municipis", "https://example.test", TerritorialPublicationMode.FullSnapshot);

        Assert.Throws<DomainRuleException>(() => source.Approve(Guid.NewGuid(), DateTimeOffset.UtcNow));

        source.RecordLegalTerms("Open data", "https://example.test/license", "INE", true, true);
        source.Approve(Guid.NewGuid(), DateTimeOffset.UtcNow);
        Assert.Equal(TerritorialSourceApprovalStatus.Approved, source.ApprovalStatus);
    }

    private static TerritorialUnit NewUnit() =>
        new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
}
