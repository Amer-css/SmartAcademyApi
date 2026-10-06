using Microsoft.AspNetCore.Http;
using SmartAcademyApi.Services;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SmartAcademyApi.AuditMiddlewar
{
    public class AuditMiddleware
    {
        private readonly RequestDelegate _next;

        public AuditMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, IAuditService auditService)
        {
            // 1. التقاط بيانات الطلب قبل معالجته
            var method = context.Request.Method;
            var path = context.Request.Path;
            var ipAddress = context.Connection.RemoteIpAddress?.ToString();

            // جلب معرف المستخدم إذا كان مسجل دخول (من الـ JWT Token)
            var userId = context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "Anonymous";

            // تحديد اسم الحدث بناءً على المسار أو الطريقة (يمكن تخصيصه لاحقاً)
            var action = $"{method} {path}";

            // 2. السماح للطلب بمتابعة طريقه إلى الـ Controller وتنفيذ العملية
            await _next(context);

            // 3. التقاط كود الاستجابة (StatusCode) بعد أن ينتهي الـ Controller من الرد
            var statusCode = context.Response.StatusCode;

            // 4. حفظ السجل في قاعدة البيانات بشكل غير متزامن
            // (نستعمل الـ RequestServices لجلب الـ AuditService لأن الـ Middleware له دورة حياة مختلفة Singleton)
            await auditService.LogAsync(userId, action, path, method, ipAddress, statusCode);
        }


    }
}
