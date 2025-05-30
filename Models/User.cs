using System;
using System.ComponentModel.DataAnnotations;

namespace EduSync.Backend.Models
{
    public class User
    {
        [Key]
        public Guid UserId { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Role { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        // Navigation properties
        public ICollection<Course>? Courses { get; set; } = new List<Course>(); // For instructors
        public ICollection<Result>? Results { get; set; } = new List<Result>(); // For students
        public ICollection<Enrollment>? Enrollments { get; set; } = new List<Enrollment>(); // For students
    }
}
