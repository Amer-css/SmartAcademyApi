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
            .Where(u => u.IsActive == true) // Fetch only active users (ignoring soft-deleted ones)
            .Select(u => new                
            {
              u.Id,
              u.FullName,
              u.Email,
              u.Role,
              u.IsActive
            })
                .ToListAsync();

            return Ok(users);
        }

        //   [HttpDelete("Delete/{id}")]
        //  [EnableRateLimiting("WriteLimiter")]
        /// <summary>
        /// Deactivates a user account (Soft Delete) by setting IsActive to false.
        /// </summary>
        /// <param name="id">The ID of the user to deactivate.</param>
        /// <returns>Success message if deactivated.</returns>
        /// <response code="200">User deactivated successfully.</response>
        /// <response code="404">User not found.</response>
        [HttpDelete("Delete user/{id}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> DeleteStudent(int id)
        {
            // Find the student user by ID
            var student = await _context.Users.FindAsync(id);
            if (student == null)
            {
                return NotFound(new { message = "Student not found." });
            }

            // Optional: Check if the student has active enrollments before deactivation
            bool hasActiveEnrollments = await _context.Enrollments
                .AnyAsync(e => e.StudentId == id && e.IsActive == true);

            // If you want to prevent deactivating a student who has active courses:
            if (hasActiveEnrollments)
            {
                // You can either block it or just deactivate their account and handle enrollments
                // Here we choose to inform the admin or unenroll them first
                return BadRequest(new { message = "Cannot deactivate student with active course enrollments." });
            }


            // Apply soft delete by deactivating the account
            student.IsActive = false;

            _context.Users.Update(student);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Student account has been deactivated successfully." });
        }


     
        [HttpPost("restore user /{id}")]
        public async Task<IActionResult> RestoreUser(int id)
        {
            var user = await _context.Users.IgnoreQueryFilters()
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                return NotFound("User not found.");
            }

            if(user.IsActive == true)
            {
                return BadRequest("User is already active.");
            }


            user.IsActive = true;
            await _context.SaveChangesAsync();

            return Ok(new { message = "User account has been successfully restored/reactivated." });
        }




    }
}
