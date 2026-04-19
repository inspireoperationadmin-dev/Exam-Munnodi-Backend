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
        // Practice sessions do not affect analytics
        if (notification.IsPractice) return;

        var responses = await examinationApi.GetSessionResponsesAsync(notification.SessionId, ct);
        if (responses.Count == 0) return;

        var now = DateTime.UtcNow;

        // 1. Update StudentQuestionHistory (per question)
        foreach (var response in responses)
        {
            var history = await analyticsRepo.GetQuestionHistoryAsync(notification.UserId, response.QuestionId, ct);

            if (history is null)
            {
                await analyticsRepo.AddQuestionHistoryAsync(new StudentQuestionHistory
                {
                    Id               = Guid.NewGuid(),
                    UserId           = notification.UserId,
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

        // 2. Update StudentSubTopicPerformance (group by subtopic)
        var bySubTopic = responses.GroupBy(r => r.SubTopicId);

        foreach (var group in bySubTopic)
        {
            var subTopicId    = group.Key;
            var attempts      = group.Count();
            var correct       = group.Count(r => r.IsCorrect);
            var first         = group.First();

            var perf = await analyticsRepo.GetSubTopicPerformanceAsync(notification.UserId, subTopicId, ct);

            if (perf is null)
            {
                var newTotal   = attempts;
                var newCorrect = correct;
                await analyticsRepo.AddSubTopicPerformanceAsync(new StudentSubTopicPerformance
                {
                    Id                = Guid.NewGuid(),
                    UserId            = notification.UserId,
                    SubTopicId        = subTopicId,
                    TopicId           = first.TopicId,
                    SubjectId         = first.SubjectId,
                    TotalAttempts     = newTotal,
                    CorrectCount      = newCorrect,
                    CorrectPercentage = newTotal > 0 ? Math.Round((decimal)newCorrect / newTotal * 100, 2) : 0,
                    LastUpdated       = now
                }, ct);
            }
            else
            {
                perf.TotalAttempts += attempts;
                perf.CorrectCount  += correct;
                perf.CorrectPercentage = perf.TotalAttempts > 0
                    ? Math.Round((decimal)perf.CorrectCount / perf.TotalAttempts * 100, 2)
                    : 0;
                perf.LastUpdated = now;
            }
        }

        // 3. Update StudentSubjectPerformance
        if (notification.SubjectId.HasValue)
        {
            var subjectId    = notification.SubjectId.Value;
            var totalQ       = responses.Count;
            var correctQ     = responses.Count(r => r.IsCorrect);
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

                var totalCorrect = Math.Round(
                    subjectPerf.OverallCorrectPercentage / 100 * (subjectPerf.TotalQuestionsAttempted - totalQ))
                    + correctQ;
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

        await analyticsRepo.SaveChangesAsync(ct);

        // 4. Update SystemDifficulty on each question (min 5 attempts required)
        foreach (var response in responses)
        {
            var history = await analyticsRepo.GetQuestionHistoryAsync(notification.UserId, response.QuestionId, ct);
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
