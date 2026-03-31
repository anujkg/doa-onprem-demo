namespace DOA.WebApp.Auth;

/// <summary>
/// LDAP authentication service — queries on-prem Active Directory
/// </summary>
public interface ILdapAuthService
{
    LdapUserInfo? ValidateUser(string username, string password);
    List<string> GetUserGroups(string username);
    LdapUserInfo? GetUserDetails(string username);
}

public record LdapUserInfo(
    string Username,
    string DisplayName,
    string Email,
    string Department,
    List<string> Groups
);
