namespace EduSyncWebAPI.DTOs
{
    public class AssessmentCreateDTO
    {
        public string? Title { get; set; }
        public string? Questions { get; set; } 
        public int? MaxScore { get; set; }
        public Guid CourseId { get; set; }
    }

    public class AssessmentReadDTO
    {
        public Guid AssessmentId { get; set; }
        public string? Title { get; set; }
        public string? Questions { get; set; }  
        public int? MaxScore { get; set; }
        public Guid? CourseId { get; set; }
    }

    public class AssessmentSummaryDTO
    {
        public Guid AssessmentId { get; set; }
        public string? Title { get; set; }
        public int? MaxScore { get; set; }
    }

    public class AssessmentDetailDTO
    {
        public Guid AssessmentId { get; set; }
        public string? Title { get; set; }
        public int? MaxScore { get; set; }
        public string? Questions { get; set; }  
        public CourseReadDTO? Course { get; set; }
    }
}

