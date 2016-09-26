using System.Security.Claims;
using Nancy.Security;

namespace BrightstarDB.Server.Modules.Permissions
{
    public abstract class AbstractSystemPermissionsProvider
    {
        public abstract SystemPermissions GetPermissionsForUser(ClaimsPrincipal principal);

        public virtual bool HasPermissions(ClaimsPrincipal principal, SystemPermissions requestedPermissions)
        {
            return (GetPermissionsForUser(principal) & requestedPermissions) == requestedPermissions;
        }
    }
}
