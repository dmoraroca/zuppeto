using Zuppeto.Domain.Common;

namespace Zuppeto.Domain.Geography;

public enum TerritorialSourceApprovalStatus
{
    Pending,
    Approved,
    Rejected
}

public enum TerritorialPublicationMode
{
    FullSnapshot,
    Delta
}

public sealed class TerritorialDatasetSource : AggregateRoot<Guid>
{
    public TerritorialDatasetSource(
        Guid id,
        Guid countryId,
        string organisation,
        string dataset,
        string url,
        TerritorialPublicationMode publicationMode,
        string? datasetVersion = null,
        DateOnly? datasetDate = null) : base(id)
    {
        if (countryId == Guid.Empty)
        {
            throw new DomainRuleException("El país de la font territorial és obligatori.");
        }

        CountryId = countryId;
        Organisation = Required(organisation, "L'organisme de la font és obligatori.");
        Dataset = Required(dataset, "El dataset de la font és obligatori.");
        Url = Required(url, "La URL oficial de la font és obligatòria.");
        PublicationMode = publicationMode;
        DatasetVersion = NormalizeOptional(datasetVersion);
        DatasetDate = datasetDate;
    }

    public Guid CountryId { get; }
    public string Organisation { get; }
    public string Dataset { get; }
    public string Url { get; }
    public string? DatasetVersion { get; }
    public DateOnly? DatasetDate { get; }
    public TerritorialPublicationMode PublicationMode { get; }
    public string? License { get; private set; }
    public string? LicenseUrl { get; private set; }
    public string? Attribution { get; private set; }
    public bool? CommercialUseAllowed { get; private set; }
    public bool? TransformationAllowed { get; private set; }
    public string? Restrictions { get; private set; }
    public string? ThirdPartyData { get; private set; }
    public TerritorialSourceApprovalStatus ApprovalStatus { get; private set; }
    public DateTimeOffset? VerifiedAtUtc { get; private set; }
    public Guid? VerifiedByUserId { get; private set; }
    public bool IsActive { get; private set; } = true;

    public void RecordLegalTerms(
        string license,
        string? licenseUrl,
        string attribution,
        bool commercialUseAllowed,
        bool transformationAllowed,
        string? restrictions = null,
        string? thirdPartyData = null)
    {
        License = Required(license, "La llicència de la font és obligatòria.");
        LicenseUrl = NormalizeOptional(licenseUrl);
        Attribution = Required(attribution, "L'atribució de la font és obligatòria.");
        CommercialUseAllowed = commercialUseAllowed;
        TransformationAllowed = transformationAllowed;
        Restrictions = NormalizeOptional(restrictions);
        ThirdPartyData = NormalizeOptional(thirdPartyData);
    }

    public void Approve(Guid verifiedByUserId, DateTimeOffset verifiedAtUtc)
    {
        if (string.IsNullOrWhiteSpace(License) ||
            string.IsNullOrWhiteSpace(Attribution) ||
            CommercialUseAllowed is null ||
            TransformationAllowed is null)
        {
            throw new DomainRuleException("No es pot aprovar una font sense verificar la llicència i la reutilització.");
        }

        SetVerification(TerritorialSourceApprovalStatus.Approved, verifiedByUserId, verifiedAtUtc);
    }

    public void Reject(Guid verifiedByUserId, DateTimeOffset verifiedAtUtc) =>
        SetVerification(TerritorialSourceApprovalStatus.Rejected, verifiedByUserId, verifiedAtUtc);

    public void Deactivate() => IsActive = false;

    private void SetVerification(TerritorialSourceApprovalStatus status, Guid userId, DateTimeOffset timestamp)
    {
        if (userId == Guid.Empty)
        {
            throw new DomainRuleException("El responsable de la verificació és obligatori.");
        }

        ApprovalStatus = status;
        VerifiedByUserId = userId;
        VerifiedAtUtc = timestamp;
    }

    private static string Required(string value, string message)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainRuleException(message);
        }

        return value.Trim();
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
