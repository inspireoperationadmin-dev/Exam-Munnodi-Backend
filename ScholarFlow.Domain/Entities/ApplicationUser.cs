using Microsoft.AspNetCore.Identity;

namespace ScholarFlow.Domain.Entities;

public class ApplicationUser : IdentityUser<Guid>
{
    public bool IsProfileSetup { get; private set; }

    public ICollection<ExamSession> ExamSessions { get; set; } = new List<ExamSession>();

    public void MarkProfileSetup() => IsProfileSetup = true;
}