using Zuppeto.Application.Auth;
using Zuppeto.Application.Commands;
using Zuppeto.Application.Results;
using Zuppeto.Application.Users;
using Zuppeto.Domain.Abstractions;

namespace Zuppeto.Application.Admin.Commands;

public sealed class SetAdminUserPasswordCommandHandler(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher)
    : ICommandHandler<SetAdminUserPasswordCommand, Result<UserDto>>
{
    public async Task<Result<UserDto>> HandleAsync(
        SetAdminUserPasswordCommand command,
        CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByIdAsync(command.UserId, cancellationToken);
        if (user is null)
        {
            return Result<UserDto>.Fail(FailureKind.NotFound, "No s’ha trobat l’usuari.");
        }

        // Same persistence as profile account update: matching new/confirm becomes users.password_hash (login password).
        user.ChangePasswordHash(passwordHasher.Hash(command.Request.NewPassword.Trim()));
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
            user.PrivacyConsent.AcceptedAtUtc));
    }
}
