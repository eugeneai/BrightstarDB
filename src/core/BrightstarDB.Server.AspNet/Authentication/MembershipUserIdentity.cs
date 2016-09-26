using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Web.Security;

namespace BrightstarDB.Server.AspNet.Authentication
{
    public class MembershipUserIdentity : ClaimsPrincipal
    {
        private readonly MembershipUser _user;
        private readonly string[] _roles;
 
        public MembershipUserIdentity(MembershipUser user)
        {
            _user = user;
            if (Roles.Enabled)
            {
                _roles = Roles.GetRolesForUser(user.UserName);
            }
        }

        public string UserName => _user.UserName;

        public override IEnumerable<Claim> Claims 
        {
            get
            {
                return
                    (new Claim[] {new Claim(ClaimTypes.Name, UserName)})
                        .Union(_roles.Select(r => new Claim(ClaimTypes.Role, r)));
            }
        }

    }
}