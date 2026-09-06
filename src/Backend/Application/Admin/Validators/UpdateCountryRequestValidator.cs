using Zuppeto.Application.Validation;
using Zuppeto.Domain.Geography;

namespace Zuppeto.Application.Admin.Validators;

public sealed class UpdateCountryRequestValidator : IValidator<UpdateCountryRequest>
{
    public ValidationResult Validate(UpdateCountryRequest request)
    {
        var result = ValidationResult.Success();

        if (string.IsNullOrWhiteSpace(request.Code))
        {
            result.Add(nameof(request.Code), "El codi és obligatori.");
        }
        else
        {
            var code = request.Code.Trim();
            if (!CountryCodeRules.IsValid(code))
            {
                result.Add(
                    nameof(request.Code),
                    $"El codi ha de tenir entre {CountryCodeRules.MinLength} i {CountryCodeRules.MaxLength} caràcters (lletres o números).");
            }
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            result.Add(nameof(request.Name), "El nom és obligatori.");
        }

        return result;
    }
}
