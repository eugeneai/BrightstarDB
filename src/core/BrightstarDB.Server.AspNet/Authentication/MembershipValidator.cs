using System.Security.Claims;
using System.Web.Security;
using Nancy.Authentication.Basic;
using Nancy.Security;

namespace BrightstarDB.Server.AspNet.Authentication
{
    public class MembershipValidator : IUserValidator
    {
        public ClaimsPrincipal Validate(string username, string password)
        {
            return Membership.ValidateUser(username, password) ? new MembershipUserIdentity(Membership.GetUser(username)) : null;
        }
    }
}