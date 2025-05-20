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

namespace EduSyncWebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // By default, require auth
    public class ResultsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ResultsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/Results
        [AllowAnonymous]
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ResultReadDTO>>> GetResults()
        {
            var results = await _context.Results.ToListAsync();

            var resultDtos = results.Select(result => new ResultReadDTO
            {
                ResultId = result.ResultId,
                AssessmentId = result.AssessmentId,
                UserId = result.UserId,
                Score = result.Score,
                AttemptDate = result.AttemptDate
            }).ToList();

            return Ok(resultDtos);
        }

        // GET: api/Results/{id}
        [AllowAnonymous]
        [HttpGet("{id}")]
        public async Task<ActionResult<ResultReadDTO>> GetResult(Guid id)
        {
            var result = await _context.Results.FindAsync(id);

            if (result == null)
                return NotFound();

            var resultDto = new ResultReadDTO
            {
                ResultId = result.ResultId,
                AssessmentId = result.AssessmentId,
                UserId = result.UserId,
                Score = result.Score,
                AttemptDate = result.AttemptDate
            };

            return Ok(resultDto);
        }

        // POST: api/Results
        [HttpPost]
        public async Task<ActionResult<ResultReadDTO>> PostResult(ResultCreateDTO dto)
        {
            var userIdFromToken = User.FindFirst("sub")?.Value;
            var userRole = User.FindFirst("role")?.Value;

            // Students can only create their own results
            if (userRole == "student" && dto.UserId.ToString() != userIdFromToken)
            {
                return Forbid("Students can only add results for themselves.");
            }

            var result = new Result
            {
                ResultId = Guid.NewGuid(),
                AssessmentId = dto.AssessmentId,
                UserId = dto.UserId,
                Score = dto.Score,
                AttemptDate = dto.AttemptDate
            };

            _context.Results.Add(result);
            await _context.SaveChangesAsync();

            var resultDto = new ResultReadDTO
            {
                ResultId = result.ResultId,
                AssessmentId = result.AssessmentId,
                UserId = result.UserId,
                Score = result.Score,
                AttemptDate = result.AttemptDate
            };

            return CreatedAtAction(nameof(GetResult), new { id = result.ResultId }, resultDto);
        }

        // PUT: api/Results/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> PutResult(Guid id, ResultCreateDTO dto)
        {
            var existingResult = await _context.Results.FindAsync(id);
            if (existingResult == null)
                return NotFound();

            var userIdFromToken = User.FindFirst("sub")?.Value;
            var userRole = User.FindFirst("role")?.Value;

            // Students can only update their own results
            if (userRole == "student" && existingResult.UserId.ToString() != userIdFromToken)
            {
                return Forbid("Students can only update their own results.");
            }

            existingResult.AssessmentId = dto.AssessmentId;
            existingResult.UserId = dto.UserId;
            existingResult.Score = dto.Score;
            existingResult.AttemptDate = dto.AttemptDate;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ResultExists(id))
                    return NotFound();
                else
                    throw;
            }

            return NoContent();
        }

        // DELETE: api/Results/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteResult(Guid id)
        {
            var result = await _context.Results.FindAsync(id);
            if (result == null)
                return NotFound();

            var userIdFromToken = User.FindFirst("sub")?.Value;
            var userRole = User.FindFirst("role")?.Value;

            // Students can only delete their own results
            if (userRole == "student" && result.UserId.ToString() != userIdFromToken)
            {
                return Forbid("Students can only delete their own results.");
            }

            _context.Results.Remove(result);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool ResultExists(Guid id)
        {
            return _context.Results.Any(e => e.ResultId == id);
        }
    }

}

