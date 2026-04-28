namespace ScholarFlow.Modules.UserProfiles.DTOs;

public sealed record StudentProfileSummaryDto(
    string                           FullName,
    string?                          StreamName,
    string?                          Medium,
    IReadOnlyList<StudentSubjectDto> Subjects);