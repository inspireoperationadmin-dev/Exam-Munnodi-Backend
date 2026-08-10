namespace ScholarFlow.Modules.UserProfiles.DTOs;

public sealed record StudentSubjectDto(
    Guid   Id,
    string Name,
    string NameEnglish,
    string? NameTamil,
    string? NameSinhala);

public sealed record StudentProfileDto(
    Guid                       Id,
    Guid                       UserId,
    string                     FullName,
    string?                    StreamName,
    string?                    Medium,
    int?                       ExamYear,
    bool                       IsSetupComplete,
    IReadOnlyList<StudentSubjectDto> Subjects);
