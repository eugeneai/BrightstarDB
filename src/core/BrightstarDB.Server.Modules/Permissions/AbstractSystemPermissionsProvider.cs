using System.Security.Claims;
using Nancy.Security;

namespace BrightstarDB.Server.Modules.Permissions
{
    public abstract class AbstractSystemPermissionsProvider
    {
        public abstract SystemPermissions GetPermissionsForUser(ClaimsPrincipal user);

        public virtual bool HasPermissions(ClaimsPrincipal user, SystemPermissions requestedPermissions)
        {
            return (GetPermissionsForUser(user) & requestedPermissions) == requestedPermissions;
        }
    }
}
