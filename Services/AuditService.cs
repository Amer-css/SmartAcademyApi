using SmartAcademyApi.Data;
using SmartAcademyApi.Models;
using  System;
using System.Threading.Tasks;

namespace SmartAcademyApi.Services
{
    public class AuditService : IAuditService
    {
        private readonly AppDbContext _context;

        public AuditService(AppDbContext context)
        {
            _context = context;
        }

        public async Task LogAsync(string? userId, string action, string path, string method, string? ipAddress, int statusCode, string? details = null)
        {
            try
            {
                var auditLog = new AuditLog
                {
                    UserId = userId,
                    Action = action,
                    Path = path,
                    Method = method,
                    IpAddress = ipAddress,
                    StatusCode = statusCode,
                    Timestamp = DateTime.UtcNow,
                    Details = details
                };

                _context.AuditLogs.Add(auditLog);
                await _context.SaveChangesAsync();
            }
            catch (Exception)
            {
               
            }

        }
    }
}
