using System;

namespace EduSync.Backend.DTOs
{
    public class EnrollmentResponseDto
    {
        public Guid EnrollmentId { get; set; }
        public Guid UserId { get; set; }
        public Guid CourseId { get; set; }
        public DateTime EnrollmentDate { get; set; }
        public bool IsCompleted { get; set; }
    }
} 