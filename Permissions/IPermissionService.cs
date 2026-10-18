namespace SmartAcademyApi.Permissions
{
    public interface IPermissionService
    {
        bool HasPermission(ApplicationUser user, string requiredPermission);
    }
}
