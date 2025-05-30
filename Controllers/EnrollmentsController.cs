using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EduSync.Backend.Models;
using EduSync.Backend.Data;
using EduSync.API.DTOs;
using System.ComponentModel.DataAnnotations;
using EduSync.Backend.DTOs;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace EduSync.Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class EnrollmentsController : ControllerBase
    {
        private readonly ProjectDBContext _context;

        public EnrollmentsController(ProjectDBContext context)
        {
            _context = context;
        }

        // GET: api/Enrollments/student
        [HttpGet("student")]
        public async Task<ActionResult<IEnumerable<Course>>> GetStudentEnrollments()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var enrollments = await _context.Enrollments
                .Include(e => e.Course)
                .Where(e => e.UserId.ToString() == userId)
                .Select(e => e.Course)
                .ToListAsync();

            return enrollments;
        }

        // GET: api/Enrollments/check/{courseId}
        [HttpGet("check/{courseId}")]
        public async Task<ActionResult<bool>> CheckEnrollment(Guid courseId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var isEnrolled = await _context.Enrollments
                .AnyAsync(e => e.UserId.ToString() == userId && e.CourseId == courseId);

            return isEnrolled;
        }

        // GET: api/Enrollments
        [HttpGet]
        public async Task<ActionResult<IEnumerable<EnrollmentResponseDto>>> GetEnrollments()
        {
            var enrollments = await _context.Enrollments.ToListAsync();
            
            var responseDtos = enrollments.Select(e => new EnrollmentResponseDto
            {
                EnrollmentId = e.EnrollmentId,
                UserId = e.UserId,
                CourseId = e.CourseId,
                EnrollmentDate = e.EnrollmentDate,
                IsCompleted = e.IsCompleted
            });

            return Ok(responseDtos);
        }

        // GET: api/Enrollments/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<EnrollmentResponseDto>> GetEnrollment(Guid id)
        {
            var enrollment = await _context.Enrollments
                .FirstOrDefaultAsync(e => e.EnrollmentId == id);

            if (enrollment == null)
            {
                return NotFound();
            }

            var responseDto = new EnrollmentResponseDto
            {
                EnrollmentId = enrollment.EnrollmentId,
                UserId = enrollment.UserId,
                CourseId = enrollment.CourseId,
                EnrollmentDate = enrollment.EnrollmentDate,
                IsCompleted = enrollment.IsCompleted
            };

            return responseDto;
        }

        // POST: api/Enrollments
        [HttpPost]
        public async Task<ActionResult<EnrollmentResponseDto>> PostEnrollment([FromBody] EnrollmentDto enrollmentDto)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                // Verify user exists using a simple query
                var userExists = await _context.Users.AnyAsync(u => u.UserId == Guid.Parse(userId));
                if (!userExists)
                {
                    return NotFound($"User with ID {userId} not found");
                }

                // Verify course exists using a simple query
                var courseExists = await _context.Courses.AnyAsync(c => c.CourseId == enrollmentDto.CourseId);
                if (!courseExists)
                {
                    return NotFound($"Course with ID {enrollmentDto.CourseId} not found");
                }

                // Check if already enrolled using a simple query
                var isEnrolled = await _context.Enrollments
                    .AnyAsync(e => e.UserId == Guid.Parse(userId) && e.CourseId == enrollmentDto.CourseId);

                if (isEnrolled)
                {
                    return Conflict("User is already enrolled in this course");
                }

                var enrollment = new Enrollment
                {
                    EnrollmentId = Guid.NewGuid(),
                    UserId = Guid.Parse(userId),
                    CourseId = enrollmentDto.CourseId,
                    EnrollmentDate = DateTime.UtcNow,
                    IsCompleted = false
                };

                _context.Enrollments.Add(enrollment);
                await _context.SaveChangesAsync();

                var responseDto = new EnrollmentResponseDto
                {
                    EnrollmentId = enrollment.EnrollmentId,
                    UserId = enrollment.UserId,
                    CourseId = enrollment.CourseId,
                    EnrollmentDate = enrollment.EnrollmentDate,
                    IsCompleted = enrollment.IsCompleted
                };

                return CreatedAtAction("GetEnrollment", new { id = enrollment.EnrollmentId }, responseDto);
            }
            catch (Exception ex)
            {
                // Log the error
                Console.WriteLine($"Error creating enrollment: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }

                return StatusCode(500, "An error occurred while creating the enrollment");
            }
        }

        // DELETE: api/Enrollments/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteEnrollment(Guid id)
        {
            var enrollment = await _context.Enrollments.FindAsync(id);
            if (enrollment == null)
            {
                return NotFound();
            }

            _context.Enrollments.Remove(enrollment);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // GET: api/Enrollments/course/{courseId}/students
        [HttpGet("course/{courseId}/students")]
        [Authorize(Roles = "Instructor")]
        public async Task<ActionResult<IEnumerable<StudentDto>>> GetEnrolledStudents(Guid courseId)
        {
            try
            {
                var enrollments = await _context.Enrollments
                    .Include(e => e.User)
                    .Where(e => e.CourseId == courseId)
                    .Select(e => new StudentDto
                    {
                        UserId = e.User.UserId,
                        Name = e.User.Name,
                        Email = e.User.Email,
                        EnrollmentDate = e.EnrollmentDate,
                        IsCompleted = e.IsCompleted
                    })
                    .ToListAsync();

                return Ok(enrollments);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred while fetching enrolled students");
            }
        }

        private bool EnrollmentExists(Guid id)
        {
            return _context.Enrollments.Any(e => e.EnrollmentId == id);
        }
    }
}