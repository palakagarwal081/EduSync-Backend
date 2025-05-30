using System.ComponentModel.DataAnnotations;

namespace EduSync.Backend.DTOs
{
    public class CourseDto
    {
        public Guid CourseId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public Guid InstructorId { get; set; }
        public string InstructorName { get; set; } = string.Empty;
        public string MediaUrl { get; set; } = string.Empty;
        public int EnrollmentCount { get; set; }
        public int AssessmentCount { get; set; }
        public string CourseContent { get; set; } = string.Empty;
        public DateTime LastUpdated { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsEnrolled { get; set; }
    }

    public class CreateCourseDto
    {
        [Required]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;

        [Required]
        public Guid InstructorId { get; set; }

        [Required]
        public string MediaUrl { get; set; } = string.Empty;

        public string CourseContent { get; set; } = string.Empty;
    }

    public class UpdateCourseDto
    {
        public Guid CourseId { get; set; }

        [Required]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;

        [Required]
        public Guid InstructorId { get; set; }

        [Required]
        public string MediaUrl { get; set; } = string.Empty;

        public string CourseContent { get; set; } = string.Empty;
    }
}
