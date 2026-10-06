using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using SmartAcademyApi.Data;
using Microsoft.AspNetCore.RateLimiting;
namespace SmartAcademyApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]

    public class AdminController : ControllerBase
    {
        private readonly AppDbContext _context;


        public AdminController(AppDbContext context)
        {
            _context = context;
        }


        public class UpdateRoleDto
        {
            public string NewRole { get; set; } // مثل: "Instructor" أو "Admin"
        }


        [HttpPut("users/{id}/role")]
        [EnableRateLimiting("WriteLimiter")]
        public async Task<IActionResult> UpdateUserRole(int id, [FromBody] UpdateRoleDto model)
        {
            var allowedRoles = new[] { "Student", "Instructor", "Admin" };
            if (string.IsNullOrEmpty(model.NewRole) || !allowedRoles.Contains(model.NewRole))
            {
                return BadRequest("The entered role is invalid. The allowed roles are: Student, Instructor, Admin.");
            }

            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound("User not found.");
            }

            var OldRole =user.Role;
            // C. Updating the role and saving changes
            user.Role = model.NewRole;
            await _context.SaveChangesAsync();

            return Ok(new { message = $"The user role has been updated from {OldRole} To '{user.Role} successfully." });

        }



        [HttpGet("GatAllUsers")]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _context.Users
                .Select(u => new
                {
                    u.Id,
                    u.FullName,
                    u.Email,
                    u.Role
                })
                .ToListAsync();

            return Ok(users);
        }





    }
}
