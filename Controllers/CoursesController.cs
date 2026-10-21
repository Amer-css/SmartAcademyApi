
using System.Diagnostics;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using SmartAcademyApi.Data;
using SmartAcademyApi.DTOs;
using SmartAcademyApi.Models;
using SmartAcademyApi.Permissions;


namespace SmartAcademyApi.Controllers
{
    //Cash Keys
    public static class CacheKeys
    {
        public static readonly string Courses = "courses_cache_key";
        public static readonly string Instructors = "instructors_cache_key";
        
    }


    [Route("api/[controller]")]
    [ApiController]
    public class CoursesController: Controller
    {
        private readonly AppDbContext _context;

        private readonly IMemoryCache _cache;

        public CoursesController(AppDbContext context, IMemoryCache cache)
        {
            _context = context;
            _cache = cache;
        }
        //using cacheing
        [HttpGet("Get All Course")]      
        public async Task<IActionResult> GetCourses()
        {



            // Attempt to retrieve from cache; if not found or expired, execute the factory delegate to fetch from DB and cache the result
            var courses = await _cache.GetOrCreateAsync(CacheKeys.Courses, async entry =>
            {
                // Set the absolute expiration time for this cache entry (e.g., 5 minutes)
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
               
                // Fetch courses from the database along with their instructor details
                var fetchedCourses = await _context.Courses
                    .AsNoTracking()
                    .Select(c => new
                    {
                        c.Id,
                        c.Title,
                        c.Description,
                        c.Price,
                        InstructorName = c.Instructor.FullName
                    })
                    .ToListAsync();

              
                // Return the fresh data so GetOrCreateAsync automatically stores it in the RAM cache
                return fetchedCourses;
            });

   
            return Ok(courses);
        }

        //------------



        ////with out cacheing
        //[HttpGet("Get All Course")]
        //public async Task<IActionResult> GetCourses()
        //{
        //    var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        //    // Fetching data directly from the database on every request.
        //    var courses = await _context.Courses
        //        .Include(c => c.Instructor) //Retrieve data for the instructor who created the course.
        //        .Select(c => new
        //        {
        //            c.Id,
        //            c.Title,
        //            c.Description,
        //            c.Price,
        //            InstructorName = c.Instructor.FullName
        //        })
        //        .ToListAsync();

        //    stopwatch.Stop();
        //    Console.WriteLine($"[Without Cache] Execution Time: {stopwatch.ElapsedMilliseconds} ms");

        //    return Ok(courses);
        //}

        //[HttpGet("{id}")]
        //public async Task<IActionResult> GetCourseByID(int id)
        //{
        //    var course = await _context.Courses
        //        .Include(c => c.Instructor)
        //        .Include(c => c.Lessons) // Retrieve the lessons associated with this course.
        //        .Where(c => c.Id == id ) 
        //        .Where(c => c.Id == id)
        //        .Select(c => new
        //        {
        //            c.Id,
        //            c.Title,
        //            c.Description,
        //            c.Price,
        //            InstructorName = c.Instructor.FullName,
        //            Lessons = c.Lessons.Select(l => new { l.Id, l.Title, l.VideoUrl })
        //        })
        //        .FirstOrDefaultAsync();

        //    if (course == null)
        //    {
        //        return NotFound("The course does not exist.");
        //    }

        //    return Ok(course);
        //}

        [HttpPost]
        [HasPermission(SmartAcademyApi.Permissions.Permissions.Courses.Create)]
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

            // Remove the old version from the cache.
            _cache.Remove(CacheKeys.Courses);

            return Ok(new { message = "The course has been successfully created", courseId = course.Id });
        }

        [HttpDelete("Delete course/{id}")]
        [HasPermission(SmartAcademyApi.Permissions.Permissions.Courses.Delete)]
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

            // Remove the old version from the cache.
            _cache.Remove(CacheKeys.Courses);
            return Ok(new { message = "Course has been deleted successfully." });
        }

    
        [HttpPost("restore course/{id}")]
        [HasPermission(SmartAcademyApi.Permissions.Permissions.Courses.Edit)]
        public async Task<IActionResult> RestoreCourse(int id)
        {
            var course = await _context.Courses
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(c => c.Id == id);

            if (course == null)
            {
                return NotFound("Course not found.");
            }

            if (course.IsActive == true)
            {
                return BadRequest("Course is already active.");
            }

            course.IsActive = true;
            await _context.SaveChangesAsync();


            // Remove the old version from the cache.
            _cache.Remove(CacheKeys.Courses);

            return Ok(new { message = "Course has been successfully restored/reactivated." });
        }


    }
}
