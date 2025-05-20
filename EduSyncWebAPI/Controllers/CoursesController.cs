using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EduSyncWebAPI.Data;
using EduSyncWebAPI.Models;
using EduSyncWebAPI.DTOs;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace EduSyncWebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Require authentication globally
    public class CoursesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CoursesController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/Courses
        [AllowAnonymous]
        [HttpGet]
        public async Task<ActionResult<IEnumerable<CourseReadDTO>>> GetCourses()
        {
            var courses = await _context.Courses.ToListAsync();

            var courseDtos = courses.Select(c => new CourseReadDTO
            {
                CourseId = c.CourseId,
                Title = c.Title,
                Description = c.Description,
                InstructorId = c.InstructorId,
                MediaUrl = c.MediaUrl
            }).ToList();

            return Ok(courseDtos);
        }

        // GET: api/Courses/{id}
        [AllowAnonymous]
        [HttpGet("{id}")]
        public async Task<ActionResult<CourseDetailDTO>> GetCourse(Guid id)
        {
            var course = await _context.Courses
                .Include(c => c.Assessments)
                .Include(c => c.Instructor)
                .FirstOrDefaultAsync(c => c.CourseId == id);

            if (course == null)
                return NotFound();

            var courseDetailDto = new CourseDetailDTO
            {
                CourseId = course.CourseId,
                Title = course.Title,
                Description = course.Description,
                InstructorId = course.InstructorId,
                MediaUrl = course.MediaUrl,
                Assessments = course.Assessments?.Select(a => new AssessmentSummaryDTO
                {
                    AssessmentId = a.AssessmentId,
                    Title = a.Title,
                    MaxScore = a.MaxScore
                }).ToList(),
                Instructor = course.Instructor == null ? null : new UserDto
                {
                    UserId = course.Instructor.UserId,
                    FullName = course.Instructor.Name,
                    Email = course.Instructor.Email
                }
            };

            return Ok(courseDetailDto);
        }

        // PUT: api/Courses/{id}
        [Authorize(Roles = "Instructor")]
        [HttpPut("{id}")]
        public async Task<IActionResult> PutCourse(Guid id, CourseCreateDTO courseDto)
        {
            var course = await _context.Courses.FindAsync(id);
            if (course == null)
                return NotFound();

            var currentUserId = GetCurrentUserId();
            if (course.InstructorId.ToString() != currentUserId)
                return Forbid("You can only update your own courses.");

            course.Title = courseDto.Title;
            course.Description = courseDto.Description;
            course.InstructorId = courseDto.InstructorId;
            course.MediaUrl = courseDto.MediaUrl;

            _context.Entry(course).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CourseExists(id))
                    return NotFound();
                else
                    throw;
            }

            return NoContent();
        }

        // POST: api/Courses
        [Authorize(Roles = "Instructor")]
        [HttpPost]
        public async Task<ActionResult<CourseReadDTO>> PostCourse(CourseCreateDTO courseDto)
        {
            var currentUserId = GetCurrentUserId();

            // You might want to enforce that InstructorId matches current user
            if (courseDto.InstructorId.ToString() != currentUserId)
                return Forbid("You can only create courses assigned to yourself.");

            var course = new Course
            {
                CourseId = Guid.NewGuid(),
                Title = courseDto.Title,
                Description = courseDto.Description,
                InstructorId = courseDto.InstructorId,
                MediaUrl = courseDto.MediaUrl
            };

            _context.Courses.Add(course);
            await _context.SaveChangesAsync();

            var resultDto = new CourseReadDTO
            {
                CourseId = course.CourseId,
                Title = course.Title,
                Description = course.Description,
                InstructorId = course.InstructorId,
                MediaUrl = course.MediaUrl
            };

            return CreatedAtAction(nameof(GetCourse), new { id = course.CourseId }, resultDto);
        }

        // DELETE: api/Courses/{id}
        [Authorize(Roles = "Instructor")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCourse(Guid id)
        {
            var course = await _context.Courses.FindAsync(id);
            if (course == null)
                return NotFound();

            var currentUserId = GetCurrentUserId();
            if (course.InstructorId.ToString() != currentUserId)
                return Forbid("You can only delete your own courses.");

            _context.Courses.Remove(course);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool CourseExists(Guid id)
        {
            return _context.Courses.Any(e => e.CourseId == id);
        }

        // Helper method to get current user ID from JWT token claims
        private string GetCurrentUserId()
        {
            // Assumes "sub" claim contains the user ID (adjust if your claim is different)
            return User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        }
    }
}
