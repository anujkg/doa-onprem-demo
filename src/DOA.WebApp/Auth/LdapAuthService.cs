using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;

namespace DOA.WebApp.Auth;

/// <summary>
/// LDAP authentication service — connects to on-prem Active Directory
/// Uses System.DirectoryServices (Windows-only, on-prem dependency)
/// 
/// ⚠️ MIGRATION TARGET: Replace with Microsoft Entra ID + MSAL
/// </summary>
public class LdapAuthService : ILdapAuthService
{
    private readonly string _ldapServer;
    private readonly int _ldapPort;
    private readonly string _baseDn;
    private readonly string _serviceAccount;
    private readonly string _servicePassword;

    public LdapAuthService(string ldapServer, int ldapPort, string baseDn,
        string serviceAccount, string servicePassword)
    {
        _ldapServer = ldapServer;
        _ldapPort = ldapPort;
        _baseDn = baseDn;
        _serviceAccount = serviceAccount;
        _servicePassword = servicePassword;
    }

    public LdapUserInfo? ValidateUser(string username, string password)
    {
        try
        {
            var ldapPath = $"{_ldapServer}:{_ldapPort}/{_baseDn}";

            using var entry = new DirectoryEntry(ldapPath, username, password);
            using var searcher = new DirectorySearcher(entry)
            {
                Filter = $"(&(objectClass=user)(sAMAccountName={username}))"
            };

            searcher.PropertiesToLoad.Add("displayName");
            searcher.PropertiesToLoad.Add("mail");
            searcher.PropertiesToLoad.Add("department");
            searcher.PropertiesToLoad.Add("memberOf");

            var result = searcher.FindOne();
            if (result == null) return null;

            var displayName = result.Properties["displayName"][0]?.ToString() ?? username;
            var email = result.Properties["mail"][0]?.ToString() ?? "";
            var department = result.Properties["department"][0]?.ToString() ?? "";
            var groups = new List<string>();

            foreach (var group in result.Properties["memberOf"])
            {
                groups.Add(group?.ToString() ?? "");
            }

            Console.WriteLine($"[{DateTime.Now}] LDAP auth success: {username}");
            return new LdapUserInfo(username, displayName, email, department, groups);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[{DateTime.Now}] LDAP auth failed for {username}: {ex.Message}");
            return null;
        }
    }

    public List<string> GetUserGroups(string username)
    {
        var groups = new List<string>();

        try
        {
            using var context = new PrincipalContext(
                ContextType.Domain,
                _ldapServer.Replace("ldap://", ""),
                _baseDn,
                _serviceAccount,
                _servicePassword);

            using var user = UserPrincipal.FindByIdentity(context, username);
            if (user == null) return groups;

            foreach (var group in user.GetAuthorizationGroups())
            {
                groups.Add(group.Name);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[{DateTime.Now}] LDAP group lookup failed: {ex.Message}");
        }

        return groups;
    }

    public LdapUserInfo? GetUserDetails(string username)
    {
        try
        {
            var ldapPath = $"{_ldapServer}:{_ldapPort}/{_baseDn}";

            using var entry = new DirectoryEntry(ldapPath, _serviceAccount, _servicePassword);
            using var searcher = new DirectorySearcher(entry)
            {
                Filter = $"(&(objectClass=user)(sAMAccountName={username}))"
            };

            searcher.PropertiesToLoad.Add("displayName");
            searcher.PropertiesToLoad.Add("mail");
            searcher.PropertiesToLoad.Add("department");
            searcher.PropertiesToLoad.Add("memberOf");

            var result = searcher.FindOne();
            if (result == null) return null;

            var displayName = result.Properties["displayName"][0]?.ToString() ?? username;
            var email = result.Properties["mail"][0]?.ToString() ?? "";
            var department = result.Properties["department"][0]?.ToString() ?? "";
            var groups = new List<string>();

            foreach (var group in result.Properties["memberOf"])
            {
                groups.Add(group?.ToString() ?? "");
            }

            return new LdapUserInfo(username, displayName, email, department, groups);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[{DateTime.Now}] LDAP user lookup failed: {ex.Message}");
            return null;
        }
    }
}
