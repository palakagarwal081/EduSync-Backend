using Microsoft.AspNetCore.Mvc;
using EduSync.Backend.Services;
using Microsoft.AspNetCore.Authorization;
using System.Text;

namespace EduSync.Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FilesController : ControllerBase
    {
        private readonly BlobStorageService _blobStorageService;
        private readonly ILogger<FilesController> _logger;

        public FilesController(BlobStorageService blobStorageService, ILogger<FilesController> logger)
        {
            _blobStorageService = blobStorageService;
            _logger = logger;
        }

        [HttpGet("test")]
        public IActionResult Test()
        {
            _logger.LogInformation("Test endpoint called");
            return Ok("API is working");
        }

        [HttpPost("upload")]
        [Authorize(Roles = "Instructor")]
        public async Task<IActionResult> SaveCourseUrls(string courseId, [FromBody] CourseUrlsDto urls)
        {
            _logger.LogInformation($"SaveCourseUrls called for courseId: {courseId}");
            
            try
            {
                if (string.IsNullOrEmpty(courseId))
                {
                    return BadRequest("CourseId is required");
                }

                if (urls == null)
                {
                    return BadRequest("No URLs provided");
                }

                // Save course content URL if provided
                if (!string.IsNullOrEmpty(urls.CourseContentUrl))
                {
                    _logger.LogInformation($"Saving course content URL for course {courseId}");
                    using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(urls.CourseContentUrl)))
                    {
                        await _blobStorageService.SaveCourseContentUrlAsync(courseId, stream);
                    }
                }

                // Save media URL if provided
                if (!string.IsNullOrEmpty(urls.MediaUrl))
                {
                    _logger.LogInformation($"Saving media URL for course {courseId}");
                    using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(urls.MediaUrl)))
                    {
                        await _blobStorageService.SaveMediaUrlAsync(courseId, stream);
                    }
                }

                return Ok(new { courseId, urls });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error saving course URLs: {ex.Message}");
                return StatusCode(500, "An error occurred while saving the course URLs");
            }
        }

        [HttpGet("{courseId}")]
        public async Task<IActionResult> GetCourseUrls(string courseId)
        {
            try
            {
                var (courseContentUrl, mediaUrl) = await _blobStorageService.GetCourseUrlsAsync(courseId);
                return Ok(new { courseContentUrl, mediaUrl });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error retrieving course URLs for course {courseId}: {ex.Message}");
                return StatusCode(500, "An error occurred while retrieving the course URLs");
            }
        }

        [HttpDelete("{courseId}")]
        [Authorize(Roles = "Instructor")]
        public async Task<IActionResult> DeleteCourseUrls(string courseId)
        {
            try
            {
                var result = await _blobStorageService.DeleteCourseUrlsAsync(courseId);
                if (result)
                {
                    return Ok();
                }
                return NotFound("Course URLs not found");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error deleting course URLs for course {courseId}: {ex.Message}");
                return StatusCode(500, "An error occurred while deleting the course URLs");
            }
        }
    }

    public class CourseUrlsDto
    {
        public string CourseContentUrl { get; set; }
        public string MediaUrl { get; set; }
    }
} 