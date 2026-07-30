using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarFlow.Domain.Entities;
using ScholarFlow.Domain.Events;
using ScholarFlow.Domain.Interfaces;

namespace ScholarFlow.Modules.UserProfiles.EventHandlers;

public sealed class RecordStudyActivityOnExamSessionCompletedHandler(IApplicationDbContext db)
    : INotificationHandler<ExamSessionCompletedDomainEvent>
{
    public async Task Handle(ExamSessionCompletedDomainEvent notification, CancellationToken ct)
    {
        var alreadyRecorded = await db.StudentStudyActivities
            .AnyAsync(activity => activity.SessionId == notification.SessionId, ct);

        if (alreadyRecorded)
        {
            return;
        }

        var questionCount = await db.UserResponses
            .CountAsync(response => response.SessionId == notification.SessionId, ct);

        db.StudentStudyActivities.Add(StudentStudyActivity.Create(
            notification.UserId,
            GetSriLankaToday(),
            notification.SessionId,
            notification.SubjectId,
            notification.Mode,
            questionCount));

        await db.SaveChangesAsync(ct);
    }

    private static DateTime GetSriLankaToday()
    {
        var now = DateTime.UtcNow;

        try
        {
            var timeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Colombo");
            return TimeZoneInfo.ConvertTimeFromUtc(now, timeZone).Date;
        }
        catch (TimeZoneNotFoundException)
        {
            var timeZone = TimeZoneInfo.FindSystemTimeZoneById("Sri Lanka Standard Time");
            return TimeZoneInfo.ConvertTimeFromUtc(now, timeZone).Date;
        }
    }
}
