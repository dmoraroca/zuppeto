using Zuppeto.Application.Commands;
using Zuppeto.Application.Results;
using Zuppeto.Application.Users;

namespace Zuppeto.Application.Admin.Commands;

public sealed record UpdateAdminUserCommand(Guid UserId, UpdateAdminUserRequest Request)
    : ICommand<Result<UserDto>>;
