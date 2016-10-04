using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

namespace BrightstarDB.Server.Modules.Tests
{
    public sealed class MockUserIdentity : ClaimsPrincipal
    {
        public MockUserIdentity(string userName, IEnumerable<string> roles):base()
        {
            if (userName != null)
            {
                AddIdentity(new ClaimsIdentity(new Claim[] { new Claim(ClaimTypes.Name, userName) }, "MockAuthentication"));
            }
            var nonNullRoles = roles.Where(r => r != null).ToList();
            if (nonNullRoles.Any())
            {
                AddIdentity(new ClaimsIdentity(nonNullRoles.Select(r => new Claim(ClaimTypes.Role, r)), "MockAuthentication"));
            }
        }

    }
}