using System.ComponentModel.DataAnnotations;

namespace EduSync.Backend.DTOs
{
    public class ResultDto
    {
        public Guid ResultId { get; set; }
        public Guid AssessmentId { get; set; }
        public string AssessmentTitle { get; set; } = string.Empty;
        public string SubmittedAnswers { get; set; } = string.Empty;
        public Guid UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public int Score { get; set; }
        public DateTime AttemptDate { get; set; }
        public DateTime SubmittedAt { get; set; }
    }

    public class CreateResultDto
    {
        [Required]
        public Guid AssessmentId { get; set; }

        [Required]
        public string SubmittedAnswers { get; set; } = string.Empty;

        [Required]
        public int Score { get; set; }
    }

    public class UpdateResultDto
    {
        [Required]
        public Guid ResultId { get; set; }

        [Required]
        public string SubmittedAnswers { get; set; } = string.Empty;

        [Required]
        public int Score { get; set; }
    }
}
