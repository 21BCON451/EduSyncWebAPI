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
    [Authorize]  // All endpoints require authentication by default
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _context;

        public UsersController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/Users
        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserDto>>> GetUsers()
        {
            var users = await _context.Users.ToListAsync();

            var userDtos = users.Select(u => new UserDto
            {
                UserId = u.UserId,
                FullName = u.Name,
                Email = u.Email
            }).ToList();

            return Ok(userDtos);
        }

        // GET: api/Users/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<UserDetailDto>> GetUser(Guid id)
        {
            var user = await _context.Users
                .Include(u => u.Courses)
                .FirstOrDefaultAsync(u => u.UserId == id);

            if (user == null)
                return NotFound();

            var userDetailDto = new UserDetailDto
            {
                UserId = user.UserId,
                FullName = user.Name,
                Email = user.Email,
                Courses = user.Courses?.Select(c => new CourseReadDTO
                {
                    CourseId = c.CourseId,
                    Title = c.Title,
                    Description = c.Description,
                    InstructorId = c.InstructorId,
                    MediaUrl = c.MediaUrl
                }).ToList()
            };

            return Ok(userDetailDto);
        }

        // PUT: api/Users/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> PutUser(Guid id, UserDto userDto)
        {
            // Allow only the user themselves OR instructor to update user details
            var userIdFromToken = User.FindFirst("sub")?.Value; // assuming JWT sub = UserId
            var userRole = User.FindFirst("role")?.Value;

            if (id.ToString() != userIdFromToken && userRole != "instructor")
            {
                return Forbid("You can only update your own profile or be an instructor.");
            }

            if (id != userDto.UserId)
                return BadRequest("User ID mismatch");

            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound();

            user.Name = userDto.FullName;
            user.Email = userDto.Email;

            _context.Entry(user).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!UserExists(id))
                    return NotFound();
                else
                    throw;
            }

            return NoContent();
        }

        // POST: api/Users
        [AllowAnonymous]
        [HttpPost]
        public async Task<ActionResult<UserDto>> PostUser(UserDto userDto)
        {
            var user = new User
            {
                UserId = Guid.NewGuid(),
                Name = userDto.FullName,
                Email = userDto.Email,
                Role = "student" // Default role assigned
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var createdUserDto = new UserDto
            {
                UserId = user.UserId,
                FullName = user.Name,
                Email = user.Email
            };

            return CreatedAtAction(nameof(GetUser), new { id = user.UserId }, createdUserDto);
        }

        // DELETE: api/Users/{id}
        [Authorize(Roles = "instructor")]  // Only instructor can delete
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(Guid id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound();

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool UserExists(Guid id)
        {
            return _context.Users.Any(e => e.UserId == id);
        }
    }

}
