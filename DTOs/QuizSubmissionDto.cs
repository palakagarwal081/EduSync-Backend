namespace EduSync.API.DTOs
{
    public class QuizSubmissionDto
    {
        public Guid AssessmentId { get; set; }
        public List<SubmittedAnswerDto> Answers { get; set; }
        public int Score { get; set; }
    }

    public class SubmittedAnswerDto
    {
        public int QuestionIndex { get; set; }
        public string Answer { get; set; }
    }
}