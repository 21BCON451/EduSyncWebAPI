using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
    [Authorize] // Require authentication for all actions unless explicitly allowed
    public class AssessmentsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AssessmentsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/Assessments
        [AllowAnonymous]
        [HttpGet]
        public async Task<ActionResult<IEnumerable<AssessmentDto>>> GetAssessments()
        {
            var assessments = await _context.Assessments.ToListAsync();
            var assessmentsDto = assessments.Select(a => new AssessmentDto
            {
                AssessmentId = a.AssessmentId,
                Title = a.Title,
                MaxScore = a.MaxScore,
                CourseId = a.CourseId
            }).ToList();

            return Ok(assessmentsDto);
        }

        // GET: api/Assessments/5
        [AllowAnonymous]
        [HttpGet("{id}")]
        public async Task<ActionResult<AssessmentDetailDto>> GetAssessment(Guid id)
        {
            var assessment = await _context.Assessments
                .Include(a => a.Course)
                .FirstOrDefaultAsync(a => a.AssessmentId == id);

            if (assessment == null)
            {
                return NotFound();
            }

            var assessmentDetailDto = new AssessmentDetailDto
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

            return Ok(assessmentDetailDto);
        }

        // PUT: api/Assessments/5
        [Authorize(Roles = "Instructor")]
        [HttpPut("{id}")]
        public async Task<IActionResult> PutAssessment(Guid id, AssessmentDto assessmentDto)
        {
            if (id != assessmentDto.AssessmentId)
            {
                return BadRequest();
            }

            var assessment = await _context.Assessments
                .Include(a => a.Course)
                .FirstOrDefaultAsync(a => a.AssessmentId == id);

            if (assessment == null)
            {
                return NotFound();
            }

            var currentUserId = GetCurrentUserId();
            if (assessment.Course == null || assessment.Course.InstructorId.ToString() != currentUserId)
            {
                return Forbid("You can only update assessments of your own courses.");
            }

            // Update fields
            assessment.Title = assessmentDto.Title;
            assessment.MaxScore = assessmentDto.MaxScore;
            assessment.CourseId = assessmentDto.CourseId; // Consider validating this too to ensure it belongs to user

            _context.Entry(assessment).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!AssessmentExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/Assessments
        [Authorize(Roles = "Instructor")]
        [HttpPost]
        public async Task<ActionResult<AssessmentDto>> PostAssessment(AssessmentDto assessmentDto)
        {
            var currentUserId = GetCurrentUserId();

            // Validate that Course belongs to current instructor
            var course = await _context.Courses.FindAsync(assessmentDto.CourseId);
            if (course == null)
                return BadRequest("Course does not exist.");

            if (course.InstructorId.ToString() != currentUserId)
                return Forbid("You can only add assessments to your own courses.");

            var assessment = new Assessment
            {
                AssessmentId = Guid.NewGuid(),
                Title = assessmentDto.Title,
                MaxScore = assessmentDto.MaxScore,
                CourseId = assessmentDto.CourseId,
                Questions = "" // Adjust if your DTO supports questions
            };

            _context.Assessments.Add(assessment);
            await _context.SaveChangesAsync();

            assessmentDto.AssessmentId = assessment.AssessmentId;

            return CreatedAtAction(nameof(GetAssessment), new { id = assessment.AssessmentId }, assessmentDto);
        }

        // DELETE: api/Assessments/5
        [Authorize(Roles = "Instructor")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAssessment(Guid id)
        {
            var assessment = await _context.Assessments
                .Include(a => a.Course)
                .FirstOrDefaultAsync(a => a.AssessmentId == id);

            if (assessment == null)
            {
                return NotFound();
            }

            var currentUserId = GetCurrentUserId();
            if (assessment.Course == null || assessment.Course.InstructorId.ToString() != currentUserId)
            {
                return Forbid("You can only delete assessments of your own courses.");
            }

            _context.Assessments.Remove(assessment);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool AssessmentExists(Guid id)
        {
            return _context.Assessments.Any(e => e.AssessmentId == id);
        }

        // Helper to get current logged-in user id from JWT claims
        private string GetCurrentUserId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        }
    }
}

