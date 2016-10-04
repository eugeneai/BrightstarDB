using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using System.Web.Security;

namespace BrightstarDB.Server.AspNet.Authentication
{
    public class MembershipUserIdentity : ClaimsPrincipal
    {
        public MembershipUserIdentity(MembershipUser user):base(new GenericIdentity(user.UserName))
        {
            if (Roles.Enabled)
            {
                var roles = Roles.GetRolesForUser(user.UserName);
                if (roles.Any())
                {
                    AddIdentity(new ClaimsIdentity(roles.Select(r => new Claim(ClaimTypes.Role, r)), "ASPNET Roles Provider"));
                }
            }
        }
    }
}