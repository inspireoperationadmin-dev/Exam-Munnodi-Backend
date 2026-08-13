namespace ScholarFlow.Modules.Analytics.DTOs.Admin;

public sealed record AdminStudentWeeklyTrendDto(
    DateTime WeekStartDate,
    int ExamCount,
    decimal AverageScore);
