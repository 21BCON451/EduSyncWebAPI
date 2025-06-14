using EduSyncWebAPI.Data;
using EduSyncWebAPI.DTOs;
using EduSyncWebAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace EduSyncWebAPI.Controllers
{
    [Authorize] 
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
            try
            {
              
                var userIdFromToken = User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;
                var userRole = User.FindFirst("http://schemas.microsoft.com/ws/2008/06/identity/claims/role")?.Value?.ToLower();

                Console.WriteLine($" Token UserId: {userIdFromToken}");
                Console.WriteLine($" DTO UserId: {userDto.UserId}");
                Console.WriteLine($" Route ID: {id}");
                Console.WriteLine($" Role: {userRole}");

                
                if (string.IsNullOrEmpty(userIdFromToken))
                {
                    Console.WriteLine("❌ Missing UserId claim from token.");
                    return Unauthorized("Missing UserId in token.");
                }

                // Only allow user to update their own profile unless instructor
                if (id.ToString() != userIdFromToken && userRole != "instructor")
                {
                    Console.WriteLine("❌ Forbidden: Not allowed to edit another user's profile.");
                    return Forbid("You can only update your own profile or be an instructor.");
                }

                // Validate that route ID matches payload ID
                if (id != userDto.UserId)
                {
                    Console.WriteLine("❌ ID mismatch.");
                    return BadRequest("User ID mismatch between route and payload.");
                }

                // Find the user
                var user = await _context.Users.FindAsync(id);
                if (user == null)
                {
                    Console.WriteLine("❌ User not found in database.");
                    return NotFound("User not found.");
                }

                // Update fields
                user.Name = userDto.FullName ?? user.Name;
                user.Email = userDto.Email ?? user.Email;

                _context.Entry(user).State = EntityState.Modified;

                await _context.SaveChangesAsync();

                Console.WriteLine(" User profile updated.");
                return NoContent();
            }
            catch (Exception ex)
            {
                Console.WriteLine($" Server exception: {ex.Message}");
                return StatusCode(500, $"Server error: {ex.Message}");
            }
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
                Role = "student"
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
        [Authorize(Roles = "instructor")]  
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
