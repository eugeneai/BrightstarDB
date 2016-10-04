using System.Security.Claims;
using Nancy.Security;

namespace BrightstarDB.Server.Modules.Permissions
{
    /// <summary>
    /// A permissions provider that provides the union of permissions from two other providers
    /// </summary>
    public class CombiningSystemPermissionsProvider : AbstractSystemPermissionsProvider
    {
        private readonly AbstractSystemPermissionsProvider _first;
        private readonly AbstractSystemPermissionsProvider _second;

        public CombiningSystemPermissionsProvider(AbstractSystemPermissionsProvider first,
                                                  AbstractSystemPermissionsProvider second)
        {
            _first = first;
            _second = second;
        }

        public override SystemPermissions GetPermissionsForUser(ClaimsPrincipal user)
        {
            return _first.GetPermissionsForUser(principal) | _second.GetPermissionsForUser(principal);
        }

        public override bool HasPermissions(ClaimsPrincipal user, SystemPermissions requestedPermissions)
        {
            return _first.HasPermissions(principal, requestedPermissions) ||
                   _second.HasPermissions(principal, requestedPermissions);
        }
    }
}