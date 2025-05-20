using System;
using System.Collections.Generic;

namespace EduSyncWebAPI.Models;

public partial class Assessment
{
    public Guid AssessmentId { get; set; }

    public Guid? CourseId { get; set; }

    public string? Title { get; set; }

    public string? Questions { get; set; }

    public int? MaxScore { get; set; }

    public virtual Course? Course { get; set; }

    public virtual ICollection<Result> Results { get; set; } = new List<Result>();
}
