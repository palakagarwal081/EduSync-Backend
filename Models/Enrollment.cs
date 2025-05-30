using System.ComponentModel.DataAnnotations;

namespace EduSync.Backend.Models
{
    public class Enrollment
    {
        [Key]
        public Guid EnrollmentId { get; set; }

        [Required]
        public Guid UserId { get; set; }
        public User? User { get; set; }

        [Required]
        public Guid CourseId { get; set; }
        public Course? Course { get; set; }

        public DateTime EnrollmentDate { get; set; } = DateTime.UtcNow;
        public bool IsCompleted { get; set; }
    }
}
