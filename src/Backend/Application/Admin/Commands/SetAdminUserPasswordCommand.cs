using Zuppeto.Application.Commands;
using Zuppeto.Application.Results;
using Zuppeto.Application.Users;

namespace Zuppeto.Application.Admin.Commands;

public sealed record SetAdminUserPasswordCommand(Guid UserId, SetAdminUserPasswordRequest Request)
    : ICommand<Result<UserDto>>;
