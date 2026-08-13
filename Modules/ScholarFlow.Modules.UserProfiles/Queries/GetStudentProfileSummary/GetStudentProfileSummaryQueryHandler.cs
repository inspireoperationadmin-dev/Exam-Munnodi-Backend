using MediatR;
using ScholarFlow.Domain.Enums;
using ScholarFlow.Domain.Interfaces;
using ScholarFlow.Domain.Interfaces.Repositories;
using ScholarFlow.Modules.UserProfiles.DTOs;
using ScholarFlow.SharedKernel.Exceptions;

namespace ScholarFlow.Modules.UserProfiles.Queries.GetStudentProfileSummary;

public sealed class GetStudentProfileSummaryQueryHandler(
    IStudentProfileRepository profileRepo,
    ICurrentUser              currentUser)
    : IRequestHandler<GetStudentProfileSummaryQuery, StudentProfileSummaryDto>
{
    public async Task<StudentProfileSummaryDto> Handle(
        GetStudentProfileSummaryQuery request, CancellationToken ct)
    {
        var profile = await profileRepo.GetByUserIdWithDetailsAsync(currentUser.UserId, ct)
            ?? throw new NotFoundException("Student profile not found.");

        return new StudentProfileSummaryDto(
            FullName:   profile.FullName,
            StreamName: profile.AcademicStream is null
                ? null
                : SelectName(profile.Medium, profile.AcademicStream.NameEnglish, profile.AcademicStream.NameTamil, profile.AcademicStream.NameSinhala),
            Medium:     profile.Medium?.ToString(),
            Subjects:   profile.SubjectSelections
                .Select(ss => new StudentSubjectDto(
                    ss.SubjectId,
                    SelectName(profile.Medium, ss.Subject.NameEnglish, ss.Subject.NameTamil, ss.Subject.NameSinhala),
                    ss.Subject.NameEnglish,
                    ss.Subject.NameTamil,
                    ss.Subject.NameSinhala))
                .ToList());
    }

    private static string SelectName(PaperMedium? medium, string english, string? tamil, string? sinhala)
        => medium switch
        {
            PaperMedium.Tamil when !string.IsNullOrWhiteSpace(tamil) => tamil,
            PaperMedium.Sinhala when !string.IsNullOrWhiteSpace(sinhala) => sinhala,
            _ => english
        };
}
