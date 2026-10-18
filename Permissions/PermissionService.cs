namespace SmartAcademyApi.Permissions
{
    public class PermissionService : IPermissionService
    {
        // Does the user have the required permission or not?
        public bool HasPermission(ApplicationUser user, string requiredPermission)
        {
            if (user == null || user.Permissions == null) return false;

            // Verify whether the requested result falls within the user's permissions
            return user.Permissions.Contains(requiredPermission);
        }
    }
}
