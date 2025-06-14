using EduSyncWebAPI.Data;
using EduSyncWebAPI.DTOs;
using EduSyncWebAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json; 

namespace EduSyncWebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class AssessmentsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AssessmentsController(AppDbContext context)
        {
            _context = context;
        }

        private string GetUserIdFromToken()
        {
            return User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier || c.Type == "sub")?.Value!;
        }

        // GET: api/Assessments
        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<AssessmentReadDTO>>> GetAssessments()
        {
            var assessments = await _context.Assessments.ToListAsync();
            var assessmentsDto = assessments.Select(a => new AssessmentReadDTO
            {
                AssessmentId = a.AssessmentId,
                Title = a.Title,
                MaxScore = a.MaxScore,
                CourseId = a.CourseId,
                Questions = a.Questions
            }).ToList();

            return Ok(assessmentsDto);
        }

        // GET: api/Assessments/5
        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<ActionResult<AssessmentDetailDTO>> GetAssessment(Guid id)
        {
            var assessment = await _context.Assessments
                .Include(a => a.Course)
                .FirstOrDefaultAsync(a => a.AssessmentId == id);

            if (assessment == null)
                return NotFound();

            var dto = new AssessmentDetailDTO
            {
                AssessmentId = assessment.AssessmentId,
                Title = assessment.Title,
                MaxScore = assessment.MaxScore,
                Questions = assessment.Questions,
                Course = assessment.Course == null ? null : new CourseReadDTO
                {
                    CourseId = assessment.Course.CourseId,
                    Title = assessment.Course.Title,
                    Description = assessment.Course.Description,
                    InstructorId = assessment.Course.InstructorId,
                    MediaUrl = assessment.Course.MediaUrl
                }
            };

            return Ok(dto);
        }

        // POST: api/Assessments
        [HttpPost]
        [Authorize(Roles = "Instructor")]
        public async Task<ActionResult<AssessmentReadDTO>> PostAssessment(AssessmentCreateDTO dto)
        {
            var trainerId = GetUserIdFromToken();

            var course = await _context.Courses.FindAsync(dto.CourseId);
            if (course == null)
                return NotFound("Course not found.");

            if (User.IsInRole("Trainer") && course.InstructorId.ToString() != trainerId)
                return Forbid("You are not authorized to add an assessment to this course.");

           
            try
            {
                JsonDocument.Parse(dto.Questions);
            }
            catch (JsonException)
            {
                return BadRequest("Questions field must be a valid JSON string.");
            }

            var assessment = new Assessment
            {
                AssessmentId = Guid.NewGuid(),
                Title = dto.Title,
                MaxScore = dto.MaxScore,
                Questions = dto.Questions,
                CourseId = dto.CourseId
            };

            _context.Assessments.Add(assessment);
            await _context.SaveChangesAsync();

            var result = new AssessmentReadDTO
            {
                AssessmentId = assessment.AssessmentId,
                Title = assessment.Title,
                MaxScore = assessment.MaxScore,
                CourseId = assessment.CourseId,
                Questions = assessment.Questions
            };

            return CreatedAtAction(nameof(GetAssessment), new { id = assessment.AssessmentId }, result);
        }

        // PUT: api/Assessments/{id}
        [HttpPut("{id}")]
        [Authorize(Roles = "Instructor")]
        public async Task<IActionResult> PutAssessment(Guid id, AssessmentCreateDTO dto)
        {
            var userId = GetUserIdFromToken();

            var assessment = await _context.Assessments.Include(a => a.Course).FirstOrDefaultAsync(a => a.AssessmentId == id);
            if (assessment == null)
                return NotFound();

            if (User.IsInRole("Trainer") && assessment.Course?.InstructorId.ToString() != userId)
                return Forbid("You are not authorized to update this assessment.");

           
            try
            {
                JsonDocument.Parse(dto.Questions);
            }
            catch (JsonException)
            {
                return BadRequest("Questions field must be a valid JSON string.");
            }

            assessment.Title = dto.Title;
            assessment.Questions = dto.Questions;
            assessment.MaxScore = dto.MaxScore;

            _context.Entry(assessment).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/Assessments/{id}
        [HttpDelete("{id}")]
        [Authorize(Roles = "Instructor")]
        public async Task<IActionResult> DeleteAssessment(Guid id)
        {
            var userId = GetUserIdFromToken();

            var assessment = await _context.Assessments.Include(a => a.Course).FirstOrDefaultAsync(a => a.AssessmentId == id);
            if (assessment == null)
                return NotFound();

            if (User.IsInRole("Trainer") && assessment.Course?.InstructorId.ToString() != userId)
                return Forbid("You are not authorized to delete this assessment.");

            _context.Assessments.Remove(assessment);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool AssessmentExists(Guid id)
        {
            return _context.Assessments.Any(e => e.AssessmentId == id);
        }
    }
}