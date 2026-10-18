using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using SmartAcademyApi.Data;
using SmartAcademyApi.Models;
using SmartAcademyApi.DTOs;
using Microsoft.AspNetCore.RateLimiting;


namespace SmartAcademyApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EnrollmentsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public EnrollmentsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost("{courseId}")]
        [Authorize(Roles = "Student")]
        [EnableRateLimiting("WriteLimiter")]
        public async Task<IActionResult> EnrollInCourse(int courseId)
        {
            // a. Securely extract the current student ID from the JWT token
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                               ?? User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.NameId)?.Value;

            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int studentId))
            {
                return Unauthorized("Unable to identify the user");
            }
           
            // b. Check if the course already exists in the database.
            var courseExists = await _context.Courses.AnyAsync(c => c.Id == courseId);
            if (!courseExists)
            {
                return NotFound("The requested course does not exist.");
            }

            // C. Check if the student is already registered for this course? (Prevent duplicates)
            var alreadyEnrolled = await _context.Enrollments
                .AnyAsync(e => e.StudentId == studentId && e.CourseId == courseId);

            if (alreadyEnrolled)
            {
                return BadRequest("You are already registered for this course.");
            }

            // D. Create the new registration record
            var enrollment = new Enrollment
            {
                StudentId = studentId,
                CourseId = courseId,
                IsActive = true
            };

            _context.Enrollments.Add(enrollment);
            await _context.SaveChangesAsync();

            return Ok(new { message = "You have successfully registered for the course!" });
        }

        // 2. View courses for the currently registered student
        [HttpGet("my-courses")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> GetMyEnrolledCourses()
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                               ?? User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.NameId)?.Value;

            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int studentId))
            {
                return Unauthorized("Unable to identify the user");
            }

            var enrolledCourses = await _context.Enrollments
                .Where(e => e.StudentId == studentId)
                .Include(e => e.Course)
                .ThenInclude(c => c.Instructor)
                .Select(e => new
                {
                    e.Course.Id,
                    e.Course.Title,
                    e.Course.Description,
                    e.Course.Price,
                    InstructorName = e.Course.Instructor.FullName,
                    e.EnrolledAt
                })
                .ToListAsync();

            return Ok(enrolledCourses);
        }



        [HttpPost("enroll")]
        [Authorize(Roles = "Student")]
        [EnableRateLimiting("WriteLimiter")]
        public async Task<IActionResult> EnrollInCourse([FromBody] EnrollDto modle)
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if(string.IsNullOrEmpty(userIdString))
               return Unauthorized();

            int studentId = int.Parse(userIdString);

            // 2. Verify that the requested course actually exists in the system.
            var courseExists = await _context.Courses.AnyAsync(c => c.Id == modle.CourseId);
            if (!courseExists)
            {
                return NotFound(new { message = "The specified course does not exist." });
            }

            // 3. Verify that the student is not already registered for the same course (prevent duplicates)
            var alreadyEnrolled = await _context.Enrollments.AnyAsync(e => e.StudentId == studentId && e.CourseId == modle.CourseId);
            if(alreadyEnrolled)
            {
               return BadRequest(new { message = "You are already enrolled in this course." });
            }

            // 4. Successfully create the registration process
            var enrollment = new Enrollment
            {
                StudentId = studentId,
                CourseId = modle.CourseId,
                EnrolledAt = DateTime.UtcNow,
                IsActive = true
            };

            _context.Enrollments.Add(enrollment);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Successfully enrolled in the course." });
        }

        [HttpPost("uneroll")]
        [Authorize(Roles = "Admin")]
        [EnableRateLimiting("WriteLimiter")]
        public async Task<IActionResult> UnenrollStudentByCourse(int studentId, int courseId)
        {
            // Find the active enrollment matching both student and course
            var enrollment = await _context.Enrollments
            .FirstOrDefaultAsync(e => e.StudentId == studentId && e.CourseId == courseId && e.IsActive == true);

            if (enrollment == null)
            {
                return NotFound(new { message = "No active enrollment found for this student in the specified course." });
            }

            // Deactivate the enrollment
            enrollment.IsActive = false;

            _context.Enrollments.Update(enrollment);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Enrollment has been deactivated successfully." });
        }

    }
}
