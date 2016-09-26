using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

namespace BrightstarDB.Server.Modules.Tests
{
    public class MockUserIdentity : ClaimsPrincipal
    {
        public MockUserIdentity(string userName, string[] claims)
        {
            var allClaims = new List<Claim> {new Claim(ClaimTypes.Name, userName)};
            allClaims.AddRange(claims.Select(c=>new Claim(c, c)));
            AddIdentity(new ClaimsIdentity(allClaims));
        }
    }
}