using Zuppeto.Application.Commands;
using Zuppeto.Application.Factories;
using Zuppeto.Application.Results;
using Zuppeto.Application.Users;
using Zuppeto.Domain.Abstractions;
using Zuppeto.Application.TerritorialImports;

namespace Zuppeto.Application.Admin.Commands;

public sealed class UpdateAdminUserCommandHandler(
    IUserRepository userRepository,
    IUserProfileFactory userProfileFactory,
    TerritorialLocationService territorialLocations)
    : ICommandHandler<UpdateAdminUserCommand, Result<UserDto>>
{
    public async Task<Result<UserDto>> HandleAsync(
        UpdateAdminUserCommand command,
        CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByIdAsync(command.UserId, cancellationToken);
        if (user is null)
        {
            return Result<UserDto>.Fail(FailureKind.NotFound, "No s’ha trobat l’usuari.");
        }

        var request = command.Request;
        var avatarUrl = string.IsNullOrWhiteSpace(request.AvatarUrl) ? null : request.AvatarUrl.Trim();
        var city = request.City.Trim();
        var country = request.Country.Trim();
        var countryId = user.TerritorialCountryId;
        var unitId = user.TerritorialUnitId;
        try
        {
            if ((request.CountryId is null) != (request.TerritorialUnitId is null))
                return Result<UserDto>.Fail(FailureKind.Conflict, "El país i la localitat territorial s’han d’informar conjuntament.");
            if (request.CountryId is not null)
            {
                var location = await territorialLocations.ResolveSelectionAsync(request.CountryId.Value, request.TerritorialUnitId!.Value, cancellationToken);
                city = location.Locality;
                country = location.Country;
                countryId = location.CountryId;
                unitId = location.TerritorialUnitId;
            }
        }
        catch (Exception exception) when (exception is KeyNotFoundException or InvalidOperationException)
        {
            return Result<UserDto>.Fail(FailureKind.Conflict, exception.Message);
        }

        user.ReplaceProfile(
            userProfileFactory.Create(
                request.DisplayName.Trim(),
                city,
                country,
                request.Comments.Trim(),
                avatarUrl));
        user.SetTerritorialLocation(countryId, unitId);

        await userRepository.UpdateAsync(user, cancellationToken);

        return Result<UserDto>.Success(new UserDto(
            user.Id,
            user.Email,
            user.Role,
            user.Profile.DisplayName,
            user.Profile.City,
            user.Profile.Country,
            user.Profile.Comments,
            user.Profile.AvatarUrl,
            user.PrivacyConsent.Accepted,
            user.PrivacyConsent.AcceptedAtUtc,
            user.HasLocalCredential,
            user.IsTotpEnabled,
            user.TerritorialCountryId,
            user.TerritorialUnitId));
    }
}
