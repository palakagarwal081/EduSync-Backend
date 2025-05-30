using Microsoft.AspNetCore.Mvc;
using EduSync.Backend.Data;
using EduSync.Backend.Models;
using Microsoft.EntityFrameworkCore;
using EduSync.Backend.DTOs;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace EduSync.Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AssessmentsController : ControllerBase
    {
        private readonly ProjectDBContext _context;

        public AssessmentsController(ProjectDBContext context)
        {
            _context = context;
        }

        // GET: api/Assessments
        [HttpGet]
        public async Task<ActionResult<IEnumerable<AssessmentDto>>> GetAssessments()
        {
            return await _context.Assessments
                .Include(a => a.Course)
                .Select(a => new AssessmentDto
                {
                    AssessmentId = a.AssessmentId,
                    Title = a.Title,
                    Questions = a.Questions,
                    CourseId = a.CourseId,
                    CourseTitle = a.Course.Title
                })
                .ToListAsync();
        }

        // GET: api/Assessments/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<AssessmentDto>> GetAssessment(Guid id)
        {
            var assessment = await _context.Assessments
                .Include(a => a.Course)
                .FirstOrDefaultAsync(a => a.AssessmentId == id);

            if (assessment == null)
            {
                return NotFound();
            }

            return new AssessmentDto
            {
                AssessmentId = assessment.AssessmentId,
                Title = assessment.Title,
                Questions = assessment.Questions,
                CourseId = assessment.CourseId,
                CourseTitle = assessment.Course.Title
            };
        }

        [HttpGet("byCourse/{courseId}")]
        public async Task<ActionResult<IEnumerable<AssessmentDto>>> GetAssessmentsByCourse(Guid courseId)
        {
            var assessments = await _context.Assessments
                .Where(a => a.CourseId == courseId)
                .Include(a => a.Course)
                .Select(a => new AssessmentDto
                {
                    AssessmentId = a.AssessmentId,
                    CourseId = a.CourseId,
                    Title = a.Title,
                    MaxScore = a.MaxScore,
                    Questions = a.Questions,
                    CourseTitle = a.Course.Title
                })
                .ToListAsync();

            return assessments;
        }

        [Authorize(Roles = "Instructor")]
        [HttpGet("my")]
        public async Task<ActionResult<IEnumerable<AssessmentDto>>> GetMyAssessments()
        {
            var instructorId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (instructorId == null)
                return BadRequest("User ID not found in token.");

            if (!Guid.TryParse(instructorId, out Guid instructorIdGuid))
                return BadRequest("Invalid instructor ID format.");

            var assessments = await _context.Assessments
                .Include(a => a.Course)
                .Where(a => a.Course.InstructorId == instructorIdGuid)
                .Select(a => new AssessmentDto
                {
                    AssessmentId = a.AssessmentId,
                    Title = a.Title,
                    MaxScore = a.MaxScore,
                    CourseId = a.CourseId,
                    CourseTitle = a.Course.Title
                })
                .ToListAsync();

            return Ok(assessments);
        }

        // POST: api/Assessments
        [Authorize(Roles = "Instructor")]
        [HttpPost]
        public async Task<ActionResult<AssessmentDto>> PostAssessment(CreateAssessmentDto createDto)
        {
            var assessment = new Assessment
            {
                AssessmentId = Guid.NewGuid(),
                Title = createDto.Title,
                Questions = createDto.Questions,
                CourseId = createDto.CourseId,
                MaxScore = createDto.MaxScore
            };

            _context.Assessments.Add(assessment);
            await _context.SaveChangesAsync();

            // Return the newly created assessment with course details
            var createdAssessment = await _context.Assessments
                .Include(a => a.Course)
                .FirstOrDefaultAsync(a => a.AssessmentId == assessment.AssessmentId);

            if (createdAssessment == null)
            {
                return StatusCode(500, "Assessment was created but could not be retrieved");
            }

            var assessmentDto = new AssessmentDto
            {
                AssessmentId = createdAssessment.AssessmentId,
                Title = createdAssessment.Title,
                Questions = createdAssessment.Questions,
                CourseId = createdAssessment.CourseId,
                CourseTitle = createdAssessment.Course?.Title ?? string.Empty,
                MaxScore = createdAssessment.MaxScore
            };

            return CreatedAtAction("GetAssessment", new { id = assessment.AssessmentId }, assessmentDto);
        }

        // PUT: api/Assessments/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> PutAssessment(Guid id, AssessmentDto assessmentDto)
        {
            if (id != assessmentDto.AssessmentId)
            {
                return BadRequest();
            }

            var assessment = await _context.Assessments.FindAsync(id);
            if (assessment == null)
            {
                return NotFound();
            }

            assessment.Title = assessmentDto.Title;
            assessment.Questions = assessmentDto.Questions;
            assessment.CourseId = assessmentDto.CourseId;

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

        // DELETE: api/Assessments/{id}
        [HttpDelete("{id}")]
        [Authorize(Roles = "Instructor")]
        public async Task<IActionResult> DeleteAssessment(Guid id)
        {
            var assessment = await _context.Assessments
                .Include(a => a.Course)
                .FirstOrDefaultAsync(a => a.AssessmentId == id);

            if (assessment == null)
            {
                return NotFound();
            }

            // Check if the current user is the instructor of the course
            var instructorId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (instructorId == null || assessment.Course.InstructorId.ToString() != instructorId)
            {
                return Forbid();
            }

            // Remove all related results
            var results = await _context.Results
                .Where(r => r.AssessmentId == id)
                .ToListAsync();
            _context.Results.RemoveRange(results);

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
