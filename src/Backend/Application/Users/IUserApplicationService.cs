namespace YepPet.Application.Users;

public interface IUserApplicationService
{
    Task<IReadOnlyCollection<UserDto>> ListAsync(CancellationToken cancellationToken = default);

    Task<UserDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<UserDto?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task<Guid> RegisterAsync(UserRegistrationRequest request, CancellationToken cancellationToken = default);

    Task UpdateProfileAsync(UserProfileUpdateRequest request, CancellationToken cancellationToken = default);
}
