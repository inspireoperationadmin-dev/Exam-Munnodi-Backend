using Microsoft.EntityFrameworkCore;
using ScholarFlow.Domain.Entities;

namespace ScholarFlow.Domain.Interfaces;

public interface IApplicationDbContext
{
    // Identity
    DbSet<ApplicationUser> Users { get; }

    // Academic
    DbSet<AcademicStream> Streams { get; }
    DbSet<Subject> Subjects { get; }
    DbSet<SubjectStream> SubjectStreams { get; }
    DbSet<Topic> Topics { get; }
    DbSet<SubTopic> SubTopics { get; }

    // Question Bank
    DbSet<Paper> Papers { get; }
    DbSet<Question> Questions { get; }
    DbSet<Option> Options { get; }
    DbSet<Explanation> Explanations { get; }
    DbSet<ExplanationSection> ExplanationSections { get; }

    // Examination
    DbSet<ExamSession> ExamSessions { get; }
    DbSet<ExamSessionQuestion> ExamSessionQuestions { get; }
    DbSet<UserResponse> UserResponses { get; }

    // User Profiles
    DbSet<StudentProfile> StudentProfiles { get; }
    DbSet<StudentStudyActivity> StudentStudyActivities { get; }
    DbSet<StudentNotificationPreference> StudentNotificationPreferences { get; }
    DbSet<TeacherProfile> TeacherProfiles { get; }
    DbSet<StudentSubjectSelection> StudentSubjectSelections { get; }
    DbSet<StudentTeacherConnection> StudentTeacherConnections { get; }

    // Analytics
    DbSet<StudentSubTopicPerformance> StudentSubTopicPerformances { get; }
    DbSet<StudentSubjectPerformance> StudentSubjectPerformances { get; }
    DbSet<StudentQuestionHistory> StudentQuestionHistories { get; }
    DbSet<StudentTopicQuestionProgress> StudentTopicQuestionProgresses { get; }

    // Auth
    DbSet<OtpCode> OtpCodes { get; }

    // Notifications
    DbSet<NotificationDevice> NotificationDevices { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
