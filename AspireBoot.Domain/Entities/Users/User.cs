namespace AspireBoot.Domain.Entities.Users;

public class User(string email, string password) : BaseEntity(email)
{
    public string Email { get; private set; } = email;
    public string Password { get; private set; } = password;
}
