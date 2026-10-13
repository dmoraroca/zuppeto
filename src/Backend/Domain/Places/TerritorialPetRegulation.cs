using Zuppeto.Domain.Common;

namespace Zuppeto.Domain.Places;

public enum RegulationValidity { Draft, InForce, Repealed, PendingVerification }

public sealed class TerritorialPetRegulation : AggregateRoot<Guid>
{
    public TerritorialPetRegulation(Guid id, string countryCode, Guid? subdivisionId, Guid? municipalityId,
        string establishmentCategory, string title, string? article, string issuingBody, Uri officialSource,
        DateOnly publicationDate, DateOnly effectiveFrom, DateTimeOffset lastCheckedAtUtc,
        DateTimeOffset? legallyVerifiedAtUtc, RegulationValidity validity, int version) : base(id)
    {
        if (countryCode is not ("ES" or "DE")) throw new DomainRuleException("L'abast inicial de normativa és Espanya i Alemanya.");
        CountryCode = countryCode;
        SubdivisionId = subdivisionId;
        MunicipalityId = municipalityId;
        EstablishmentCategory = establishmentCategory;
        Title = title;
        Article = article;
        IssuingBody = issuingBody;
        OfficialSource = officialSource;
        PublicationDate = publicationDate;
        EffectiveFrom = effectiveFrom;
        LastCheckedAtUtc = lastCheckedAtUtc;
        LegallyVerifiedAtUtc = legallyVerifiedAtUtc;
        Validity = validity;
        Version = version;
    }
    public string CountryCode { get; }
    public Guid? SubdivisionId { get; }
    public Guid? MunicipalityId { get; }
    public string EstablishmentCategory { get; }
    public string Title { get; }
    public string? Article { get; }
    public string IssuingBody { get; }
    public Uri OfficialSource { get; }
    public DateOnly PublicationDate { get; }
    public DateOnly EffectiveFrom { get; }
    public DateTimeOffset LastCheckedAtUtc { get; }
    public DateTimeOffset? LegallyVerifiedAtUtc { get; }
    public RegulationValidity Validity { get; }
    public int Version { get; }
}

public interface ITerritorialRegulationChangeDetector
{
    Task<IReadOnlyCollection<RegulationChangeCandidate>> CheckMonthlyAsync(CancellationToken cancellationToken);
    Task<IReadOnlyCollection<RegulationChangeCandidate>> RunAnnualReviewAsync(DateOnly reviewDate, CancellationToken cancellationToken);
}

public sealed record RegulationChangeCandidate(Guid RegulationId, string SourceVersion, DateTimeOffset DetectedAtUtc,
    string Summary, bool RequiresHumanLegalReview = true);
