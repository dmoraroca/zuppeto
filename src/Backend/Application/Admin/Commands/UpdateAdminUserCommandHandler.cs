using Zuppeto.Application.Commands;
using Zuppeto.Application.Factories;
using Zuppeto.Application.Results;
using Zuppeto.Application.Users;
using Zuppeto.Domain.Abstractions;

namespace Zuppeto.Application.Admin.Commands;

public sealed class UpdateAdminUserCommandHandler(
    IUserRepository userRepository,
    IUserProfileFactory userProfileFactory)
    : ICommandHandler<UpdateAdminUserCommand, Result<UserDto>>
{
    public async Task<Result<UserDto>> HandleAsync(
        UpdateAdminUserCommand command,
        CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByIdAsync(command.UserId, cancellationToken);
        if (user is null)
        {
            return Result<UserDto>.Fail(FailureKind.NotFound, $"User '{command.UserId}' was not found.");
        }

        var request = command.Request;
        var avatarUrl = string.IsNullOrWhiteSpace(request.AvatarUrl) ? null : request.AvatarUrl.Trim();

        user.ReplaceProfile(
            userProfileFactory.Create(
                request.DisplayName.Trim(),
                request.City.Trim(),
                request.Country.Trim(),
                request.Comments.Trim(),
                avatarUrl));

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
