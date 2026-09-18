using Zuppeto.Domain.Users;

namespace Zuppeto.Application.Auth;

public interface IAuthSessionFactory
{
    Task<AuthSessionDto> CreateAsync(
        User user,
        string provider = "password",
        bool requiresProfileCompletion = false,
        CancellationToken cancellationToken = default);
}
