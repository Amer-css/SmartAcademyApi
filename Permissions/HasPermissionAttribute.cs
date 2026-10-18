using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;

namespace SmartAcademyApi.Permissions
{
    // 1. تعريف الـ Attribute ليُستخدم فوق الـ Controllers أو الـ Actions
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
    public class HasPermissionAttribute : TypeFilterAttribute
    {
        public HasPermissionAttribute(string permission) : base(typeof(PermissionAuthorizationFilter))
        {
            Arguments = new object[] { permission };
        }
    }

    // 2. تعريف "الشرطي" أو الـ Filter الذي سيفحص الصلاحية فعلياً
    public class PermissionAuthorizationFilter : IAsyncAuthorizationFilter
    {
        private readonly string _permission;
        private readonly IPermissionService _permissionService;

        public PermissionAuthorizationFilter(string permission, IPermissionService permissionService)
        {
            _permission = permission;
            _permissionService = permissionService;
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var userClaimsPrincipal = context.HttpContext.User;

            // التأكد أن المستخدم قد سجل دخوله أصلاً
            if (userClaimsPrincipal == null || !userClaimsPrincipal.Identity.IsAuthenticated)
            {
                context.Result = new UnauthorizedResult(); // 401 Unauthorized
                return;
            }

            // استخراج بيانات المستخدم من الـ Token (Claims) وتعبئتها في كلاسنا ApplicationUser
            var currentUser = new ApplicationUser
            {
                Id = userClaimsPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty,
                Roles = userClaimsPrincipal.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList(),

                // جلب الصلاحيات المخزنة في الـ Claims الخاصة بالـ Token
                Permissions = userClaimsPrincipal.FindAll("permission").Select(c => c.Value).ToList()
            };

            // فحص الصلاحية عبر الخدمة المركزية التي أنشأناها
            bool hasPermission = _permissionService.HasPermission(currentUser, _permission);

            if (!hasPermission)
            {
                context.Result = new ForbidResult(); // 403 Forbidden (ليس لديك صلاحية)
            }
        }
    }
}