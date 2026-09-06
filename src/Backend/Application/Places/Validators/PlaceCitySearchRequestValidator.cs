using Zuppeto.Application.Validation;

namespace Zuppeto.Application.Places.Validators;

public sealed class PlaceCitySearchRequestValidator : IValidator<PlaceCitySearchRequest>
{
    public ValidationResult Validate(PlaceCitySearchRequest request)
    {
        var result = ValidationResult.Success();
        var normalized = PlaceCityQueryNormalizer.Normalize(request.Q);

        if (normalized.Length < PlaceCitySearchDefaults.MinQueryLength)
        {
            result.Add(
                nameof(PlaceCitySearchRequest.Q),
                $"El paràmetre q ha de tenir almenys {PlaceCitySearchDefaults.MinQueryLength} caràcters vàlids després de normalitzar.");
        }

        if (request.Limit is < 1 or > PlaceCitySearchDefaults.MaxLimit)
        {
            result.Add(
                nameof(PlaceCitySearchRequest.Limit),
                $"El límit ha d’estar entre 1 i {PlaceCitySearchDefaults.MaxLimit}.");
        }

        return result;
    }
}
