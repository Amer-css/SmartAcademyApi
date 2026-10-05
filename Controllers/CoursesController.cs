
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
                .Include(c => c.Instructor) // جلب بيانات المدرس الذي أنشأ الكورس
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
                .Include(c => c.Lessons) // جلب الدروس التابعة لهذا الكورس
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
            // استخراج معرف المستخدم (المدرس) الحالي من الـ JWT Token
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
                InstructorId = instructorId // ربط الكورس بالمدرس الذي قام بتسجيل الدخول
            };

            _context.Courses.Add(course);
            await _context.SaveChangesAsync();

            return Ok(new { message = "The course has been successfully created", courseId = course.Id });
        }


    }
}
