using Microsoft.EntityFrameworkCore;
using ScholarFlow.Domain.Interfaces;

namespace ScholarFlow.Modules.Examination.Public;

internal sealed class ExaminationApi(IApplicationDbContext db) : IExaminationApi
{
    public async Task<IReadOnlyList<SessionResponseSummary>> GetSessionResponsesAsync(
        Guid sessionId, CancellationToken ct = default)
    {
        var responses = await db.UserResponses
            .Where(ur => ur.SessionId == sessionId)
            .Select(ur => new SessionResponseSummary(
                ur.QuestionId,
                ur.Question.SubTopicId,
                ur.Question.SubTopic.TopicId,
                ur.Question.SubTopic.Topic.SubjectId,
                ur.IsCorrect))
            .ToListAsync(ct);

        return responses.AsReadOnly();
    }
}
