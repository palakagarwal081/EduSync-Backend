using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EduSync.Backend.Data;
using EduSync.Backend.Models;
using EduSync.Backend.DTOs;
using EduSync.Backend.Services;
using System.Security.Claims;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace EduSync.Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CoursesController : ControllerBase
    {
        private readonly ProjectDBContext _context;
        private readonly ILogger<CoursesController> _logger;
        private readonly BlobStorageService _blobStorageService;

        public CoursesController(ProjectDBContext context, ILogger<CoursesController> logger, BlobStorageService blobStorageService)
        {
            _context = context;
            _logger = logger;
            _blobStorageService = blobStorageService;
        }

        // Helper method to get URLs from blob storage
        private async Task<(string mediaUrl, string courseContent)> GetUrlsFromBlobStorage(string courseId)
        {
            try
            {
                var (contentUrl, mediaUrlFromBlob) = await _blobStorageService.GetCourseUrlsAsync(courseId);
                return (mediaUrlFromBlob, contentUrl);
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"URLs not found in blob storage for course {courseId}: {ex.Message}");
                return (null, null);
            }
        }

        // GET: api/Courses
        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<CourseDto>>> GetCourses()
        {
            var coursesFromDb = await _context.Courses
                .Include(c => c.Instructor)
                .Include(c => c.Assessments)
                .Include(c => c.Enrollments)
                .ToListAsync();

            var courseDtos = new List<CourseDto>();

            foreach (var course in coursesFromDb)
            {
                var (mediaUrl, courseContent) = await GetUrlsFromBlobStorage(course.CourseId.ToString());

                courseDtos.Add(new CourseDto
                {
                    CourseId = course.CourseId,
                    Title = course.Title,
                    Description = course.Description,
                    InstructorId = course.InstructorId,
                    InstructorName = course.Instructor?.Name ?? "Unknown",
                    MediaUrl = mediaUrl ?? course.MediaUrl,
                    EnrollmentCount = course.Enrollments?.Count ?? 0,
                    AssessmentCount = course.Assessments?.Count ?? 0,
                    CourseContent = courseContent ?? course.CourseContent,
                    LastUpdated = course.LastUpdated,
                    CreatedAt = course.CreatedAt,
                    IsEnrolled = false
                });
            }

            return Ok(courseDtos);
        }

        // GET: api/Courses/available
        [HttpGet("available")]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<CourseDto>>> GetAvailableCourses()
        {
            try
            {
                _logger.LogInformation("Fetching available courses...");

                var courseCount = await _context.Courses.CountAsync();
                _logger.LogInformation($"Total courses in database: {courseCount}");

                var coursesFromDb = await _context.Courses
                    .Include(c => c.Instructor)
                    .Include(c => c.Assessments)
                    .Include(c => c.Enrollments)
                    .ToListAsync();

                var courseDtos = new List<CourseDto>();

                foreach (var course in coursesFromDb)
                {
                    var (mediaUrl, courseContent) = await GetUrlsFromBlobStorage(course.CourseId.ToString());

                    courseDtos.Add(new CourseDto
                    {
                        CourseId = course.CourseId,
                        Title = course.Title,
                        Description = course.Description,
                        InstructorId = course.InstructorId,
                        InstructorName = course.Instructor?.Name ?? "Unknown",
                        MediaUrl = mediaUrl ?? course.MediaUrl,
                        EnrollmentCount = course.Enrollments?.Count ?? 0,
                        AssessmentCount = course.Assessments?.Count ?? 0,
                        CourseContent = courseContent ?? course.CourseContent,
                        LastUpdated = course.LastUpdated,
                        CreatedAt = course.CreatedAt,
                        IsEnrolled = false
                    });
                }

                _logger.LogInformation($"Found {courseDtos.Count} courses");
                if (courseDtos.Count == 0)
                {
                    _logger.LogWarning("No courses found");
                }
                else
                {
                    _logger.LogInformation($"Course details: {JsonSerializer.Serialize(courseDtos)}");
                }

                return courseDtos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching available courses");
                return StatusCode(500, "An error occurred while fetching courses");
            }
        }

        // GET: api/Courses/5
        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<ActionResult<CourseDto>> GetCourse(Guid id)
        {
            var course = await _context.Courses
                .Include(c => c.Instructor)
                .Include(c => c.Assessments)
                .Include(c => c.Enrollments)
                .FirstOrDefaultAsync(c => c.CourseId == id);

            if (course == null)
            {
                return NotFound();
            }

            var (mediaUrl, courseContent) = await GetUrlsFromBlobStorage(id.ToString());

            var courseDto = new CourseDto
            {
                CourseId = course.CourseId,
                Title = course.Title,
                Description = course.Description,
                InstructorId = course.InstructorId,
                InstructorName = course.Instructor?.Name ?? "Unknown",
                MediaUrl = mediaUrl ?? course.MediaUrl,
                EnrollmentCount = course.Enrollments?.Count ?? 0,
                AssessmentCount = course.Assessments?.Count ?? 0,
                CourseContent = courseContent ?? course.CourseContent,
                LastUpdated = course.LastUpdated,
                CreatedAt = course.CreatedAt,
                IsEnrolled = false
            };

            return Ok(courseDto);
        }

        // GET: api/Courses/my
        [HttpGet("my")]
        [Authorize(Roles = "Instructor")]
        public async Task<ActionResult<IEnumerable<CourseDto>>> GetMyCourses()
        {
            try
            {
                _logger.LogInformation("Attempting to fetch instructor's courses");
                
                var instructorId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? 
                                 User.FindFirst("UserId")?.Value;
                
                _logger.LogInformation($"Found instructor ID in token: {instructorId}");

                if (string.IsNullOrEmpty(instructorId))
                {
                    _logger.LogWarning("User ID not found in token");
                    return BadRequest("User ID not found in token.");
                }

                if (!Guid.TryParse(instructorId, out Guid instructorIdGuid))
                {
                    _logger.LogWarning($"Invalid instructor ID format: {instructorId}");
                    return BadRequest("Invalid instructor ID format.");
                }

                var coursesFromDb = await _context.Courses
                    .Include(c => c.Instructor)
                    .Include(c => c.Assessments)
                    .Include(c => c.Enrollments)
                    .Where(c => c.InstructorId == instructorIdGuid)
                    .ToListAsync();

                var courseDtos = new List<CourseDto>();

                foreach (var course in coursesFromDb)
                {
                    var (mediaUrl, courseContent) = await GetUrlsFromBlobStorage(course.CourseId.ToString());

                    courseDtos.Add(new CourseDto
                    {
                        CourseId = course.CourseId,
                        Title = course.Title,
                        Description = course.Description,
                        InstructorId = course.InstructorId,
                        InstructorName = course.Instructor?.Name ?? "Unknown",
                        MediaUrl = mediaUrl ?? course.MediaUrl,
                        EnrollmentCount = course.Enrollments?.Count ?? 0,
                        AssessmentCount = course.Assessments?.Count ?? 0,
                        CourseContent = courseContent ?? course.CourseContent,
                        LastUpdated = course.LastUpdated,
                        CreatedAt = course.CreatedAt,
                        IsEnrolled = true
                    });
                }

                _logger.LogInformation($"Found {courseDtos.Count} courses for instructor {instructorId}");
                _logger.LogInformation($"Courses data: {JsonSerializer.Serialize(courseDtos)}");
                return Ok(courseDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching instructor's courses");
                return StatusCode(500, "An error occurred while fetching courses");
            }
        }

        // POST: api/Courses
        [HttpPost]
        [Authorize(Roles = "Instructor")]
        public async Task<ActionResult<CourseDto>> CreateCourse(CreateCourseDto createCourseDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var instructorId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(instructorId) || !Guid.TryParse(instructorId, out Guid instructorIdGuid))
            {
                return BadRequest("Invalid instructor ID");
            }

            var courseId = Guid.NewGuid();

            // Save URLs to blob storage
            try
            {
                if (!string.IsNullOrEmpty(createCourseDto.MediaUrl))
                {
                    using (var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(createCourseDto.MediaUrl)))
                    {
                        await _blobStorageService.SaveMediaUrlAsync(courseId.ToString(), stream);
                    }
                }

                if (!string.IsNullOrEmpty(createCourseDto.CourseContent))
                {
                    using (var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(createCourseDto.CourseContent)))
                    {
                        await _blobStorageService.SaveCourseContentUrlAsync(courseId.ToString(), stream);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error saving URLs to blob storage: {ex.Message}");
                return StatusCode(500, "Error saving course URLs");
            }

            var course = new Course
            {
                CourseId = courseId,
                Title = createCourseDto.Title,
                Description = createCourseDto.Description,
                InstructorId = instructorIdGuid,
                MediaUrl = createCourseDto.MediaUrl,
                CourseContent = createCourseDto.CourseContent,
                LastUpdated = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            _context.Courses.Add(course);
            await _context.SaveChangesAsync();

            var courseDto = new CourseDto
            {
                CourseId = course.CourseId,
                Title = course.Title,
                Description = course.Description,
                InstructorId = course.InstructorId,
                InstructorName = (await _context.Users.FindAsync(instructorIdGuid))?.Name ?? "Unknown",
                MediaUrl = course.MediaUrl,
                EnrollmentCount = 0,
                AssessmentCount = 0,
                CourseContent = course.CourseContent,
                LastUpdated = course.LastUpdated,
                CreatedAt = course.CreatedAt,
                IsEnrolled = false
            };

            return CreatedAtAction(nameof(GetCourse), new { id = course.CourseId }, courseDto);
        }

        // PUT: api/Courses/5
        [HttpPut("{id}")]
        [Authorize(Roles = "Instructor")]
        public async Task<IActionResult> UpdateCourse(Guid id, UpdateCourseDto updateCourseDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var course = await _context.Courses.FindAsync(id);
            if (course == null)
            {
                return NotFound();
            }

            var instructorId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(instructorId) || !Guid.TryParse(instructorId, out Guid instructorIdGuid))
            {
                return BadRequest("Invalid instructor ID");
            }

            if (course.InstructorId != instructorIdGuid)
            {
                return Forbid();
            }

            // Update URLs in blob storage
            try
            {
                if (!string.IsNullOrEmpty(updateCourseDto.MediaUrl))
                {
                    using (var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(updateCourseDto.MediaUrl)))
                    {
                        await _blobStorageService.SaveMediaUrlAsync(id.ToString(), stream);
                    }
                }

                if (!string.IsNullOrEmpty(updateCourseDto.CourseContent))
                {
                    using (var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(updateCourseDto.CourseContent)))
                    {
                        await _blobStorageService.SaveCourseContentUrlAsync(id.ToString(), stream);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating URLs in blob storage: {ex.Message}");
                return StatusCode(500, "Error updating course URLs");
            }

            course.Title = updateCourseDto.Title;
            course.Description = updateCourseDto.Description;
            course.MediaUrl = updateCourseDto.MediaUrl;
            course.CourseContent = updateCourseDto.CourseContent;
            course.LastUpdated = DateTime.UtcNow;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CourseExists(id))
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

        // DELETE: api/Courses/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "Instructor")]
        public async Task<IActionResult> DeleteCourse(Guid id)
        {
            var course = await _context.Courses.FindAsync(id);
            if (course == null)
            {
                return NotFound();
            }

            var instructorId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(instructorId) || !Guid.TryParse(instructorId, out Guid instructorIdGuid))
            {
                return BadRequest("Invalid instructor ID");
            }

            if (course.InstructorId != instructorIdGuid)
            {
                return Forbid();
            }

            _context.Courses.Remove(course);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // GET: api/Courses/test/create
        [HttpGet("test/create")]
        [AllowAnonymous]
        public async Task<ActionResult<List<CourseDto>>> CreateTestCourses()
        {
            try
            {
                var createdCourses = new List<CourseDto>();

                // First, create a test instructor if none exists
                var testInstructor = await _context.Users.FirstOrDefaultAsync(u => u.Role == "Instructor");
                if (testInstructor == null)
                {
                    testInstructor = new User
                    {
                        UserId = Guid.NewGuid(),
                        Name = "Test Instructor",
                        Email = "test.instructor@example.com",
                        PasswordHash = "test123", // This is just for testing
                        Role = "Instructor"
                    };
                    _context.Users.Add(testInstructor);
                    await _context.SaveChangesAsync();
                }

                // Create multiple test courses
                var testCourses = new[]
                {
                    new Course
                    {
                        CourseId = Guid.NewGuid(),
                        Title = "Introduction to Programming",
                        Description = "Learn the basics of programming with this comprehensive course.",
                        InstructorId = testInstructor.UserId,
                        MediaUrl = "https://example.com/video1.mp4",
                        CourseContent = "Course content goes here",
                        LastUpdated = DateTime.UtcNow,
                        CreatedAt = DateTime.UtcNow
                    },
                    new Course
                    {
                        CourseId = Guid.NewGuid(),
                        Title = "Web Development Fundamentals",
                        Description = "Master HTML, CSS, and JavaScript basics.",
                        InstructorId = testInstructor.UserId,
                        MediaUrl = "https://example.com/video2.mp4",
                        CourseContent = "Web development content",
                        LastUpdated = DateTime.UtcNow,
                        CreatedAt = DateTime.UtcNow
                    },
                    new Course
                    {
                        CourseId = Guid.NewGuid(),
                        Title = "Database Design",
                        Description = "Learn database design principles and SQL.",
                        InstructorId = testInstructor.UserId,
                        MediaUrl = "https://example.com/video3.mp4",
                        CourseContent = "Database content",
                        LastUpdated = DateTime.UtcNow,
                        CreatedAt = DateTime.UtcNow
                    }
                };

                foreach (var course in testCourses)
                {
                    _context.Courses.Add(course);
                    createdCourses.Add(new CourseDto
                    {
                        CourseId = course.CourseId,
                        Title = course.Title,
                        Description = course.Description,
                        InstructorId = course.InstructorId,
                        InstructorName = testInstructor.Name,
                        MediaUrl = course.MediaUrl,
                        EnrollmentCount = 0,
                        AssessmentCount = 0,
                        CourseContent = course.CourseContent,
                        LastUpdated = course.LastUpdated,
                        CreatedAt = course.CreatedAt,
                        IsEnrolled = false
                    });
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Created {createdCourses.Count} test courses");
                return Ok(createdCourses);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating test courses: {ex.Message}");
                return StatusCode(500, "Error creating test courses");
            }
        }

        private bool CourseExists(Guid id)
        {
            return _context.Courses.Any(e => e.CourseId == id);
        }
    }
} 