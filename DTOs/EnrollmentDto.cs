using System.ComponentModel.DataAnnotations;

namespace EduSync.Backend.DTOs
{
    public class EnrollmentDto
    {
        [Required]
        public Guid CourseId { get; set; }
    }

    public class CreateEnrollmentDto
    {
        [Required]
        public Guid UserId { get; set; }

        [Required]
        public Guid CourseId { get; set; }
    }

    public class UpdateEnrollmentDto
    {
        [Required]
        public Guid EnrollmentId { get; set; }

        [Required]
        public bool IsCompleted { get; set; }
    }
}
