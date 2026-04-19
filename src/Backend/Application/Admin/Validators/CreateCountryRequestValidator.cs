using YepPet.Application.Validation;

namespace YepPet.Application.Admin.Validators;

public sealed class CreateCountryRequestValidator : IValidator<CreateCountryRequest>
{
    public ValidationResult Validate(CreateCountryRequest request)
    {
        var result = ValidationResult.Success();

        if (string.IsNullOrWhiteSpace(request.Code))
        {
            result.Add(nameof(request.Code), "Code is required.");
        }
        else
        {
            var code = request.Code.Trim();
            if (code.Length is < 2 or > 8)
            {
                result.Add(nameof(request.Code), "Code must be between 2 and 8 characters.");
            }
            else if (!code.All(c => char.IsLetterOrDigit(c)))
            {
                result.Add(nameof(request.Code), "Code may only contain letters and digits.");
            }
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            result.Add(nameof(request.Name), "Name is required.");
        }
        else if (request.Name.Trim().Length > 200)
        {
            result.Add(nameof(request.Name), "Name is too long.");
        }

        return result;
    }
}
