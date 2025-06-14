using System;
using System.Collections.Generic;

namespace EduSyncWebAPI.Models;

public partial class User
{
    public Guid UserId { get; set; }

    public string? Name { get; set; }

    public string? Email { get; set; }

    public string? Role { get; set; }

    public string? PasswordHash { get; set; }

    public virtual ICollection<Course> Courses { get; set; } = new List<Course>();

    public virtual ICollection<Result> Results { get; set; } = new List<Result>();
}
