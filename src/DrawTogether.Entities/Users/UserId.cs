namespace DrawTogether.Entities.Users;

public sealed class UserId(string identityName)
{
    public string IdentityName { get; } = identityName;
}