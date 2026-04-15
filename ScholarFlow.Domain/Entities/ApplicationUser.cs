using Microsoft.AspNetCore.Identity;

namespace ScholarFlow.Domain.Entities;

public class ApplicationUser : IdentityUser<Guid>
{
    public ICollection<ExamSession> ExamSessions { get; set; } = new List<ExamSession>();
}
