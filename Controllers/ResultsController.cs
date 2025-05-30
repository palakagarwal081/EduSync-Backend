using Microsoft.AspNetCore.Mvc;
using EduSync.Backend.Data;
using EduSync.Backend.Models;
using Microsoft.EntityFrameworkCore;
using EduSync.Backend.DTOs;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using System.Text.Json;

namespace EduSync.Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ResultsController : ControllerBase
    {
        private readonly ProjectDBContext _context;

        public ResultsController(ProjectDBContext context)
        {
            _context = context;
        }

        // GET: api/Results
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ResultDto>>> GetResults()
        {
            var results = await _context.Results
                .Include(r => r.Assessment)
                .Include(r => r.User)
                .Select(r => new ResultDto
                {
                    ResultId = r.ResultId,
                    AssessmentId = r.AssessmentId,
                    AssessmentTitle = r.Assessment != null ? r.Assessment.Title : string.Empty,
                    SubmittedAnswers = r.SubmittedAnswers,
                    UserId = r.UserId,
                    UserName = r.User != null ? r.User.Name : string.Empty,
                    Score = r.Score,
                    AttemptDate = r.AttemptDate,
                    SubmittedAt = r.SubmittedAt
                })
                .ToListAsync();

            return results;
        }

        // GET: api/Results/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<ResultDto>> GetResult(Guid id)
        {
            var result = await _context.Results
                .Include(r => r.Assessment)
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.ResultId == id);

            if (result == null)
            {
                return NotFound();
            }

            var resultDto = new ResultDto
            {
                ResultId = result.ResultId,
                AssessmentId = result.AssessmentId,
                AssessmentTitle = result.Assessment != null ? result.Assessment.Title : string.Empty,
                SubmittedAnswers = result.SubmittedAnswers,
                UserId = result.UserId,
                UserName = result.User != null ? result.User.Name : string.Empty,
                Score = result.Score,
                AttemptDate = result.AttemptDate,
                SubmittedAt = result.SubmittedAt
            };

            return resultDto;
        }

        [Authorize]
        [HttpGet("my")]
        public async Task<IActionResult> GetMyResults()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!Guid.TryParse(userIdClaim, out Guid userId))
            {
                return BadRequest("Invalid or missing user ID in token.");
            }

            var results = await _context.Results
                .Include(r => r.Assessment)
                .Where(r => r.UserId == userId)
                .Select(r => new
                {
                    r.ResultId,
                    r.Score,
                    r.AttemptDate,
                    AssessmentTitle = r.Assessment != null ? r.Assessment.Title : "Untitled"
                })
                .ToListAsync();

            return Ok(results);
        }

        [HttpGet("byAssessment/{assessmentId}")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<ResultDto>>> GetResultsByAssessment(Guid assessmentId)
        {
            try
            {
                // First check if the assessment exists
                var assessment = await _context.Assessments
                    .Include(a => a.Course)
                    .FirstOrDefaultAsync(a => a.AssessmentId == assessmentId);

                if (assessment == null)
                {
                    return NotFound($"Assessment with ID {assessmentId} not found.");
                }

                // Get the current user's ID and role
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out Guid userId))
                {
                    return Unauthorized("Invalid user ID in token.");
                }

                // If user is an instructor, verify they own the course
                if (userRole == "Instructor" && assessment.Course.InstructorId != userId)
                {
                    return Forbid();
                }

                // Get results based on user role
                var query = _context.Results
                    .Include(r => r.User)
                    .Include(r => r.Assessment)
                    .Where(r => r.AssessmentId == assessmentId);

                // If user is a student, only show their own results
                if (userRole == "Student")
                {
                    query = query.Where(r => r.UserId == userId);
                }

                var results = await query
                    .Select(r => new ResultDto
                    {
                        ResultId = r.ResultId,
                        AssessmentId = r.AssessmentId,
                        UserId = r.UserId,
                        UserName = r.User != null ? r.User.Name : "Unknown",
                        AssessmentTitle = r.Assessment != null ? r.Assessment.Title : "Untitled",
                        Score = r.Score,
                        SubmittedAnswers = r.SubmittedAnswers,
                        AttemptDate = r.AttemptDate,
                        SubmittedAt = r.SubmittedAt != DateTime.MinValue ? r.SubmittedAt : DateTime.UtcNow
                    })
                    .ToListAsync();

                return Ok(results);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GetResultsByAssessment] Error: {ex.Message}");
                return StatusCode(500, "An error occurred while retrieving results.");
            }
        }

        // POST: api/Results
        [HttpPost]
        [Authorize]
        public async Task<ActionResult<ResultDto>> PostResult(CreateResultDto createResultDto)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out Guid userId))
            {
                return Unauthorized();
            }

            var result = new Result
            {
                ResultId = Guid.NewGuid(),
                AssessmentId = createResultDto.AssessmentId,
                SubmittedAnswers = createResultDto.SubmittedAnswers,
                UserId = userId,
                Score = createResultDto.Score,
                AttemptDate = DateTime.UtcNow,
                SubmittedAt = DateTime.UtcNow
            };

            _context.Results.Add(result);
            await _context.SaveChangesAsync();

            var assessment = await _context.Assessments.FindAsync(result.AssessmentId);
            var user = await _context.Users.FindAsync(result.UserId);

            var resultDto = new ResultDto
            {
                ResultId = result.ResultId,
                AssessmentId = result.AssessmentId,
                AssessmentTitle = assessment != null ? assessment.Title : string.Empty,
                SubmittedAnswers = result.SubmittedAnswers,
                UserId = result.UserId,
                UserName = user != null ? user.Name : string.Empty,
                Score = result.Score,
                AttemptDate = result.AttemptDate,
                SubmittedAt = result.SubmittedAt
            };

            return CreatedAtAction("GetResult", new { id = result.ResultId }, resultDto);
        }

        // PUT: api/Results/{id}
        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> PutResult(Guid id, UpdateResultDto updateResultDto)
        {
            if (id != updateResultDto.ResultId)
            {
                return BadRequest();
            }

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out Guid userId))
            {
                return Unauthorized();
            }

            var result = await _context.Results.FindAsync(id);
            if (result == null)
            {
                return NotFound();
            }

            if (result.UserId != userId)
            {
                return Forbid();
            }

            result.SubmittedAnswers = updateResultDto.SubmittedAnswers;
            result.Score = updateResultDto.Score;
            result.SubmittedAt = DateTime.UtcNow;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ResultExists(id))
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

        // DELETE: api/Results/{id}
        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteResult(Guid id)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out Guid userId))
            {
                return Unauthorized();
            }

            var result = await _context.Results.FindAsync(id);
            if (result == null)
            {
                return NotFound();
            }

            if (result.UserId != userId)
            {
                return Forbid();
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
