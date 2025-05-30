using System.ComponentModel.DataAnnotations;

namespace EduSync.Backend.DTOs
{
    public class AssessmentDto
    {
        public Guid AssessmentId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Questions { get; set; } = string.Empty;
        public Guid CourseId { get; set; }
        public string CourseTitle { get; set; } = string.Empty;
        public int MaxScore { get; set; }
    }

    public class CreateAssessmentDto
    {
        [Required]
        public Guid CourseId { get; set; }
        
        [Required]
        public string Title { get; set; } = string.Empty;
        
        [Required]
        public string Questions { get; set; } = string.Empty;
        
        [Required]
        public int MaxScore { get; set; }
    }
}
