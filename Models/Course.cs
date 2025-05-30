using System;
using System.ComponentModel.DataAnnotations;

namespace EduSync.Backend.Models
{
    public class Course
    {
        [Key]
        public Guid CourseId { get; set; }

        [Required]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;

        [Required]
        public Guid InstructorId { get; set; }
        public User? Instructor { get; set; }

        [Required]
        public string MediaUrl { get; set; } = string.Empty;

        public string CourseContent { get; set; } = string.Empty;
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<Assessment>? Assessments { get; set; } = new List<Assessment>();
        public ICollection<Enrollment>? Enrollments { get; set; } = new List<Enrollment>();
    }
}
