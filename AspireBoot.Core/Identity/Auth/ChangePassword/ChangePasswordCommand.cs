using System.Security.Claims;
using AspireBoot.Core.Cqrs;

namespace AspireBoot.Core.Identity.Auth.ChangePassword;

public record ChangePasswordCommand(string CurrentPassword, string NewPassword, ClaimsPrincipal ClaimsPrincipal)
    : ICommand;
