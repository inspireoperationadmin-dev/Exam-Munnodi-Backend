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

        await UpdateQuestionProgressAsync(notification.UserId, responses, notification.Mode, now, ct);
        await analyticsRepo.SaveChangesAsync(ct);

        foreach (var subTopicId in responses
            .Where(response => response.WasAnswered)
            .Select(r => r.SubTopicId)
            .Distinct())
        {
            await analyticsRepo.RecalculateSubTopicPerformanceAsync(notification.UserId, subTopicId, ct);
        }

        if (notification.Mode == ExamMode.TopicExam)
        {
            await analyticsRepo.SaveChangesAsync(ct);
            return;
        }

        await UpdateSubjectPerformanceAsync(notification, responses, now, ct);
        await analyticsRepo.SaveChangesAsync(ct);
        await UpdateSystemDifficultyAsync(notification.UserId, responses, ct);
    }

    private async Task UpdateQuestionProgressAsync(
        Guid userId,
        IReadOnlyList<SessionResponseSummary> responses,
        ExamMode mode,
        DateTime now,
        CancellationToken ct)
    {
        if (responses.Count == 0)
            return;

        foreach (var response in responses)
        {
            var progress = await analyticsRepo.GetQuestionProgressAsync(userId, response.QuestionId, ct);

            if (progress is null)
            {
                progress = new StudentQuestionProgress
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    QuestionId = response.QuestionId,
                    SubjectId = response.SubjectId,
                    TopicId = response.TopicId,
                    SubTopicId = response.SubTopicId,
                    LastAttemptMode = mode
                };

                await analyticsRepo.AddQuestionProgressAsync(progress, ct);
            }

            ApplyQuestionProgressAttempt(progress, response, mode, now);
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

    private static void ApplyQuestionProgressAttempt(
        StudentQuestionProgress progress,
        SessionResponseSummary response,
        ExamMode mode,
        DateTime now)
    {
        progress.SubjectId = response.SubjectId;
        progress.TopicId = response.TopicId;
        progress.SubTopicId = response.SubTopicId;
        progress.LastResponseWasAnswered = response.WasAnswered;
        progress.LastAttemptMode = mode;
        progress.LastSeenAt = now;

        if (response.WasAnswered)
        {
            progress.TimesAttempted++;
            progress.LastAnswerCorrect = response.IsCorrect;

            if (response.IsCorrect)
            {
                progress.CorrectCount++;
                progress.ConsecutiveCorrect++;
                progress.ConsecutiveWrong = 0;
            }
            else
            {
                progress.ConsecutiveCorrect = 0;
                progress.ConsecutiveWrong++;
            }
        }
        else
        {
            progress.LastAnswerCorrect = false;
        }

        progress.MasteryScore = CalculateQuestionMasteryScore(progress.ConsecutiveCorrect);
        progress.Status = progress.MasteryScore switch
        {
            >= 100 => QuestionProgressStatus.Mastered,
            > 0 => QuestionProgressStatus.Improving,
            _ => QuestionProgressStatus.NeedsRevision
        };
    }

    private static decimal CalculateQuestionMasteryScore(int consecutiveCorrect)
        => Math.Min(100m, Math.Round((decimal)consecutiveCorrect / 2 * 100, 2));

    private async Task UpdateSystemDifficultyAsync(
        Guid userId,
        IReadOnlyList<SessionResponseSummary> responses,
        CancellationToken ct)
    {
        foreach (var response in responses)
        {
            var progress = await analyticsRepo.GetQuestionProgressAsync(userId, response.QuestionId, ct);
            if (progress is null || progress.TimesAttempted < 5) continue;

            var correctRate = progress.TimesAttempted > 0
                ? (decimal)progress.CorrectCount / progress.TimesAttempted * 100
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
