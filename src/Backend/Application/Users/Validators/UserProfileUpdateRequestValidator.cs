using YepPet.Application.Validation;

namespace YepPet.Application.Users.Validators;

public sealed class UserProfileUpdateRequestValidator : IValidator<UserProfileUpdateRequest>
{
    public ValidationResult Validate(UserProfileUpdateRequest request)
    {
        var result = ValidationResult.Success();

        if (request.Id == Guid.Empty)
        {
            result.Add(nameof(request.Id), "User id is required.");
        }

        if (string.IsNullOrWhiteSpace(request.DisplayName))
        {
            result.Add(nameof(request.DisplayName), "Display name is required.");
        }

        if (request.PrivacyAccepted && request.PrivacyAcceptedAtUtc is null)
        {
            result.Add(nameof(request.PrivacyAcceptedAtUtc), "Privacy acceptance date is required.");
        }

        return result;
    }
}
