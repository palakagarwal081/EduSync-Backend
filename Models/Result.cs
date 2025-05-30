using System;
using System.ComponentModel.DataAnnotations;

namespace EduSync.Backend.Models
{
    public class Result
    {
        [Key]
        public Guid ResultId { get; set; }

        [Required]
        public Guid AssessmentId { get; set; }  // Foreign key to Assessment
        public Assessment? Assessment { get; set; }  // Navigation property

        [Required]
        public string SubmittedAnswers { get; set; } = string.Empty;

        [Required]
        public Guid UserId { get; set; }  // Foreign key to User
        public User? User { get; set; }  // Navigation property

        [Required]
        public int Score { get; set; }
        public DateTime AttemptDate { get; set; } = DateTime.UtcNow;  // Date and time of the attempt
        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

    }
}
