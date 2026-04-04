using YepPet.Application.Commands;
using YepPet.Application.Results;
using YepPet.Application.Users;

namespace YepPet.Application.Admin.Commands;

public sealed record UpdateUserRoleCommand(Guid UserId, UpdateUserRoleRequest Request) : ICommand<Result<UserDto>>;
