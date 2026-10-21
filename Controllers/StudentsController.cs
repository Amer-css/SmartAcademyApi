using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using SmartAcademyApi.Data;
using System.Security.Claims;
using SmartAcademyApi.Models;
using SmartAcademyApi.DTOs;
using Microsoft.AspNetCore.RateLimiting;
using SmartAcademyApi.Permissions;
using static SmartAcademyApi.Permissions.Permissions;

namespace SmartAcademyApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Student")]// Available only to Student
    public class StudentsController : ControllerBase
    {
        private readonly AppDbContext _context;


        public StudentsController(AppDbContext context)
        {
            _context = context;
        }
        // 1. Retrieve the student's own data and the courses they are registered for (based exclusively on the token)
        [HttpGet("my-profile")]
        public async Task<IActionResult> GetMyProfile()
        {
            // Extracting the ID from the token with complete security
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdString)) return Unauthorized();
            int studentId = int.Parse(userIdString);

            var student = await _context.Users
                .Include(u => u.Role)
                .Where(u => u.Id == studentId && u.Role.Name == "Student")
                .Include(u => u.Enrollments)
                .ThenInclude(e => e.Course)
                .Select(u => new
                {
                    u.Id,
                    u.FullName,
                    u.Email,
                    EnrolledCourses = u.Enrollments.Select(e => new
                    {
                        e.Course.Id,
                        e.Course.Title,
                        e.Course.Price,
                        e.EnrolledAt
                    })
                })
                .FirstOrDefaultAsync();

            if (student == null) return NotFound("Student not found.");

            return Ok(student);
        }

       
        // 3. Function to Update Profile Student(Name - full Name)
        [HttpPut("Updateprofile")]
        [EnableRateLimiting("WriteLimiter")]      
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto updateDto)
        {
            // 1. Securely extract the student ID exclusively from the token.
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdString))
            {
                return Unauthorized("");
            }

            int studentId = int.Parse(userIdString);

            // 2. Search for the user in the database including the Role table and verify that they are a student.
            var student = await _context.Users
                .Include(u => u.Role) // Include the Role table to verify it
                .FirstOrDefaultAsync(u => u.Id == studentId && u.Role.Name == "Student"); // Check role name via relationship

            if (student == null)
            {
                return NotFound("Student not found.");
            }

            // 3. Update only the allowed fields
            student.FullName = updateDto.FullName;

            // 4. Save changes to the database
            _context.Users.Update(student);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Profile updated successfully.",
                updatedName = student.FullName
            });
        }

        //4.Function to Update Student Password 
        [HttpPost("change-password")]
        [EnableRateLimiting("WriteLimiter")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto model)
        {
            // 1. Get the current user ID from the JWT claims
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdString))
            {
                return Unauthorized();
            }

            int studentId = int.Parse(userIdString);

            // 2. Search for the user in the database including the Role relation
            var student = await _context.Users
                .Include(u => u.Role) // Include the Role table to verify it
                .FirstOrDefaultAsync(u => u.Id == studentId && u.Role.Name == "Student"); // Check role name via relationship

            if (student == null)
            {
                return NotFound("Student not found.");
            }

            // 3. Verify that the current password is correct (using BCrypt)
            bool isPasswordValid = BCrypt.Net.BCrypt.Verify(model.CurrentPassword, student.PasswordHash);
            if (!isPasswordValid)
            {
                return BadRequest(new { message = "The current password is incorrect." });
            }

            // 4. Encrypt and save the new password
            student.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);

            _context.Users.Update(student);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Password changed successfully." });
        }



    }
}
