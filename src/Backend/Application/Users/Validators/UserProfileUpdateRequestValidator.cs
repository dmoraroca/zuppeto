using Zuppeto.Application.Validation;

namespace Zuppeto.Application.Users.Validators;

public sealed class UserProfileUpdateRequestValidator : IValidator<UserProfileUpdateRequest>
{
    public ValidationResult Validate(UserProfileUpdateRequest request)
    {
        var result = ValidationResult.Success();

        if (request.Id == Guid.Empty)
        {
            result.Add(nameof(request.Id), "Cal l’identificador d’usuari.");
        }

        if (string.IsNullOrWhiteSpace(request.DisplayName))
        {
            result.Add(nameof(request.DisplayName), "El nom visible és obligatori.");
        }
        else if (request.DisplayName.Trim().Length < 3)
        {
            result.Add(nameof(request.DisplayName), "El nom visible ha de tenir com a mínim 3 caràcters.");
        }

        if (string.IsNullOrWhiteSpace(request.City))
        {
            result.Add(nameof(request.City), "La ciutat és obligatòria.");
        }
        else if (request.City.Trim().Length < 2)
        {
            result.Add(nameof(request.City), "La ciutat ha de tenir com a mínim 2 caràcters.");
        }

        if (string.IsNullOrWhiteSpace(request.Country))
        {
            result.Add(nameof(request.Country), "El país és obligatori.");
        }
        else if (request.Country.Trim().Length < 2)
        {
            result.Add(nameof(request.Country), "El país ha de tenir com a mínim 2 caràcters.");
        }

        if (request.PrivacyAccepted && request.PrivacyAcceptedAtUtc is null)
        {
            result.Add(nameof(request.PrivacyAcceptedAtUtc), "Cal la data d’acceptació de privacitat.");
        }

        return result;
    }
}
