using System.ComponentModel.DataAnnotations;

namespace AspireBoot.Api.Identity.Users.DeleteUser;

public record DeleteUserRequest(
    [property: Required, StringLength(128, MinimumLength = 12)]
    string Password);
