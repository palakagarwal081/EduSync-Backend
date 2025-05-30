using System;
using System.ComponentModel.DataAnnotations;

namespace EduSync.Backend.Models
{
    public class Assessment
    {
        [Key]
        public Guid AssessmentId { get; set; }

        [Required]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Questions { get; set; } = string.Empty;

        [Required]
        public Guid CourseId { get; set; }
        public Course? Course { get; set; }

        public int MaxScore { get; set; }

        public ICollection<Result> Results { get; set; } = new List<Result>();
    }
}
