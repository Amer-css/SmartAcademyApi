namespace SmartAcademyApi.Services
{
    public interface IAuditService
    {
        Task LogAsync(string? userId, string action, string path, string method, string? ipAddress, int statusCode, string? details = null);
    }
}
