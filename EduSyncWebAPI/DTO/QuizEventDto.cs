using System;

namespace EduSyncWebAPI.DTOs
{
    public class QuizEventDto
    {
        public Guid ResultId { get; set; }
        public Guid UserId { get; set; }
        public Guid AssessmentId { get; set; }
        public int Score { get; set; }
        public DateTime AttemptDate { get; set; }
    }
}

