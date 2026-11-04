using System.ComponentModel.DataAnnotations;
using AspireBoot.Core.Identity.Users;
using AspireBoot.Core.Identity.Users.UpdateUser;

namespace AspireBoot.Api.Identity.Users.UpdateUser;

public record UpdateUserRequest
{
    [StringLength(100, MinimumLength = 2)]
    [RegularExpression(
        NamePolicy.AllowedPattern,
        ErrorMessage = "Name must contain only letters and spaces.",
        MatchTimeoutInMilliseconds = 100)]
    public string? NewName { get; init; }

    public IFormFile? Avatar { get; init; }

    public async Task<UpdateUserCommand> ToCommandAsync(string identityUserId, CancellationToken cancellationToken)
    {
        if (Avatar is null)
            return new UpdateUserCommand(identityUserId, NewName);

        using MemoryStream memoryStream = new((int)Avatar.Length);

        await Avatar.CopyToAsync(memoryStream, cancellationToken).ConfigureAwait(false);

        return new UpdateUserCommand(identityUserId, NewName, memoryStream.ToArray(), Avatar.ContentType);
    }
}
