
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using SmartAcademyApi.Data;
using SmartAcademyApi.DTOs;
using SmartAcademyApi.Models;

namespace SmartAcademyApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CoursesController: Controller
    {
        private readonly AppDbContext _context;

        public CoursesController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetCourses()
        {
            var courses = await _context.Courses
                .Where(c => c.IsActive)// Fetch only active courses
                .Include(c => c.Instructor) //Retrieve data for the instructor who created the course.
                .Select(c => new
                {
                    c.Id,
                    c.Title,
                    c.Description,
                    c.Price,
                    InstructorName = c.Instructor.FullName
                })
                .ToListAsync();
            return Ok(courses);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetCourseByID(int id)
        {
            var course = await _context.Courses
                .Include(c => c.Instructor)
                .Include(c => c.Lessons) // Retrieve the lessons associated with this course.
                .Where(c => c.Id == id && c.IsActive) // Ensure the course exists and is active
                .Where(c => c.Id == id)
                .Select(c => new
                {
                    c.Id,
                    c.Title,
                    c.Description,
                    c.Price,
                    InstructorName = c.Instructor.FullName,
                    Lessons = c.Lessons.Select(l => new { l.Id, l.Title, l.VideoUrl })
                })
                .FirstOrDefaultAsync();

            if (course == null)
            {
                return NotFound("The course does not exist.");
            }

            return Ok(course);
        }

        [HttpPost]
        [Authorize(Roles = "Instructor,Admin")]
        [EnableRateLimiting("WriteLimiter")]
        public async Task<IActionResult> CreateCourse([FromBody] CourseCreateDto model)
        {
            // Extract the current user (teacher) ID from the JWT token
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                               ?? User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.NameId)?.Value;

            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int instructorId))
            {
                return Unauthorized("Unable to identify the user.");
            }

            var course = new Course
            {
                Title = model.Title,
                Description = model.Description,
                Price = model.Price,
                InstructorId = instructorId // Link the course to the instructor who is logged in.
            };

            _context.Courses.Add(course);
            await _context.SaveChangesAsync();

            return Ok(new { message = "The course has been successfully created", courseId = course.Id });
        }

        [HttpDelete("Delete/{id}")]
        [Authorize(Roles ="Admin")] //Ensures only admins can delete/deactivate
        [EnableRateLimiting("WriteLimiter")]
        public async Task<IActionResult>DeleteCoures(int id)
        {
            //Find the course by iD
            var courseId = await _context.Courses.FindAsync(id);
            if (courseId == null)
            {
                return NotFound(new {message = "Course not found."});
            }

            // Check if there are active student enrollments in this course
            bool hasActiveEnrollments = await _context.Enrollments
                .AnyAsync(e => e.CourseId == id && e.IsActive == true);

            if (hasActiveEnrollments)
            {
                return BadRequest(new { message = "Cannot deactivate this course because there are active student enrollments." });
            }


            //apply soft delete by setting IsActice to false
            courseId.IsActive = false;

            _context.Courses.Update(courseId);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Course has been deleted successfully." });
        }

    }
}
