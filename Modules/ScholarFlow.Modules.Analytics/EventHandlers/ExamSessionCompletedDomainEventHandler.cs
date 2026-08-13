using MediatR;
using ScholarFlow.Domain.Entities;
using ScholarFlow.Domain.Enums;
using ScholarFlow.Domain.Events;
using ScholarFlow.Domain.Interfaces;
using ScholarFlow.Domain.Interfaces.Repositories;
using ScholarFlow.Modules.Academic.Public;

namespace ScholarFlow.Modules.Analytics.EventHandlers;

public sealed class ExamSessionCompletedDomainEventHandler(
    IAnalyticsRepository analyticsRepo,
    IExaminationApi examinationApi,
    IAcademicApi academicApi)
    : INotificationHandler<ExamSessionCompletedDomainEvent>
{
    public async Task Handle(ExamSessionCompletedDomainEvent notification, CancellationToken ct)
    {
        if (notification.Mode is not ExamMode.MockExam and not ExamMode.TopicExam)
            return;

        var responses = await examinationApi.GetSessionResponsesAsync(notification.SessionId, ct);
        if (responses.Count == 0) return;

        var now = DateTime.UtcNow;

        if (notification.Mode == ExamMode.TopicExam)
        {
            await UpdateTopicProgressAsync(notification.UserId, responses, now, ct);
            await analyticsRepo.SaveChangesAsync(ct);
            return;
        }

        await UpdateMockQuestionHistoryAsync(notification.UserId, responses, now, ct);
        await UpdateSubjectPerformanceAsync(notification, responses, now, ct);
        await analyticsRepo.SaveChangesAsync(ct);
        await UpdateSystemDifficultyAsync(notification.UserId, responses, ct);
    }

    private async Task UpdateTopicProgressAsync(
        Guid userId,
        IReadOnlyList<SessionResponseSummary> responses,
        DateTime now,
        CancellationToken ct)
    {
        var answeredResponses = responses
            .Where(response => response.WasAnswered)
            .ToList();

        if (answeredResponses.Count == 0)
            return;

        foreach (var response in answeredResponses)
        {
            var progress = await analyticsRepo.GetTopicQuestionProgressAsync(userId, response.QuestionId, ct);

            if (progress is null)
            {
                await analyticsRepo.AddTopicQuestionProgressAsync(new StudentTopicQuestionProgress
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    QuestionId = response.QuestionId,
                    SubjectId = response.SubjectId,
                    TopicId = response.TopicId,
                    SubTopicId = response.SubTopicId,
                    TimesAttempted = 1,
                    CorrectCount = response.IsCorrect ? 1 : 0,
                    LastAnswerCorrect = response.IsCorrect,
                    Status = response.IsCorrect
                        ? QuestionProgressStatus.Mastered
                        : QuestionProgressStatus.NeedsRevision,
                    LastSeenAt = now
                }, ct);
            }
            else
            {
                progress.SubjectId = response.SubjectId;
                progress.TopicId = response.TopicId;
                progress.SubTopicId = response.SubTopicId;
                progress.TimesAttempted++;
                if (response.IsCorrect) progress.CorrectCount++;
                progress.LastAnswerCorrect = response.IsCorrect;
                progress.Status = response.IsCorrect
                    ? QuestionProgressStatus.Mastered
                    : QuestionProgressStatus.NeedsRevision;
                progress.LastSeenAt = now;
            }
        }

        await analyticsRepo.SaveChangesAsync(ct);

        foreach (var subTopicId in answeredResponses.Select(r => r.SubTopicId).Distinct())
        {
            await analyticsRepo.RecalculateSubTopicPerformanceAsync(userId, subTopicId, ct);
        }
    }

    private async Task UpdateMockQuestionHistoryAsync(
        Guid userId,
        IReadOnlyList<SessionResponseSummary> responses,
        DateTime now,
        CancellationToken ct)
    {
        foreach (var response in responses)
        {
            var history = await analyticsRepo.GetQuestionHistoryAsync(userId, response.QuestionId, ct);

            if (history is null)
            {
                await analyticsRepo.AddQuestionHistoryAsync(new StudentQuestionHistory
                {
                    Id               = Guid.NewGuid(),
                    UserId           = userId,
                    QuestionId       = response.QuestionId,
                    TimesAttempted   = 1,
                    CorrectCount     = response.IsCorrect ? 1 : 0,
                    LastAnswerCorrect = response.IsCorrect,
                    LastSeenAt       = now
                }, ct);
            }
            else
            {
                history.TimesAttempted++;
                if (response.IsCorrect) history.CorrectCount++;
                history.LastAnswerCorrect = response.IsCorrect;
                history.LastSeenAt        = now;
            }
        }
    }

    private async Task UpdateSubjectPerformanceAsync(
        ExamSessionCompletedDomainEvent notification,
        IReadOnlyList<SessionResponseSummary> responses,
        DateTime now,
        CancellationToken ct)
    {
        // Fallback: derive SubjectId from question responses when it's missing on the session
        // (covers sessions created before the StartExamSession SubjectId fix)
        var effectiveSubjectId = notification.SubjectId
            ?? (responses.Count > 0 ? responses[0].SubjectId : (Guid?)null);

        if (effectiveSubjectId.HasValue)
        {
            var subjectId    = effectiveSubjectId.Value;
            var answeredResponses = responses.Where(r => r.WasAnswered).ToList();
            var totalQ       = answeredResponses.Count;
            var correctQ     = answeredResponses.Count(r => r.IsCorrect);
            var examScore    = notification.Score.Percentage;

            var subjectPerf = await analyticsRepo.GetSubjectPerformanceAsync(notification.UserId, subjectId, ct);

            if (subjectPerf is null)
            {
                await analyticsRepo.AddSubjectPerformanceAsync(new StudentSubjectPerformance
                {
                    Id                        = Guid.NewGuid(),
                    UserId                    = notification.UserId,
                    SubjectId                 = subjectId,
                    TotalExams                = 1,
                    AverageExamScore          = examScore,
                    BestScore                 = examScore,
                    TotalQuestionsAttempted   = totalQ,
                    OverallCorrectPercentage  = totalQ > 0 ? Math.Round((decimal)correctQ / totalQ * 100, 2) : 0,
                    StudyStreakDays           = 1,
                    LastStudiedAt             = now,
                    LastUpdated               = now
                }, ct);
            }
            else
            {
                // Rolling average: (oldAvg * oldCount + newScore) / (oldCount + 1)
                var newTotal              = subjectPerf.TotalExams + 1;
                subjectPerf.AverageExamScore = Math.Round(
                    (subjectPerf.AverageExamScore * subjectPerf.TotalExams + examScore) / newTotal, 2);
                subjectPerf.TotalExams    = newTotal;

                if (examScore > subjectPerf.BestScore)
                    subjectPerf.BestScore = examScore;

                subjectPerf.TotalQuestionsAttempted  += totalQ;

                var previousCorrect = (int)Math.Round(
                    subjectPerf.OverallCorrectPercentage / 100 * (subjectPerf.TotalQuestionsAttempted - totalQ));
                var totalCorrect = previousCorrect + correctQ;
                subjectPerf.OverallCorrectPercentage = subjectPerf.TotalQuestionsAttempted > 0
                    ? Math.Round((decimal)totalCorrect / subjectPerf.TotalQuestionsAttempted * 100, 2)
                    : 0;

                // Streak: if last studied yesterday → streak++, else reset to 1
                subjectPerf.StudyStreakDays = subjectPerf.LastStudiedAt.HasValue
                    && (now.Date - subjectPerf.LastStudiedAt.Value.Date).Days == 1
                    ? subjectPerf.StudyStreakDays + 1
                    : 1;

                subjectPerf.LastStudiedAt = now;
                subjectPerf.LastUpdated   = now;
            }
        }
    }

    private async Task UpdateSystemDifficultyAsync(
        Guid userId,
        IReadOnlyList<SessionResponseSummary> responses,
        CancellationToken ct)
    {
        foreach (var response in responses)
        {
            var history = await analyticsRepo.GetQuestionHistoryAsync(userId, response.QuestionId, ct);
            if (history is null || history.TimesAttempted < 5) continue;

            var correctRate = history.TimesAttempted > 0
                ? (decimal)history.CorrectCount / history.TimesAttempted * 100
                : 0;

            var systemLevel = correctRate switch
            {
                > 70  => SystemDifficultyLevel.Easy,
                >= 40 => SystemDifficultyLevel.Medium,
                _     => SystemDifficultyLevel.Hard
            };

            await academicApi.UpdateSystemDifficultyAsync(response.QuestionId, systemLevel, ct);
        }
    }
}
