using MediatR;
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
            StreamName: profile.AcademicStream?.Name,
            Subjects:   profile.SubjectSelections
                .Select(ss => new StudentSubjectDto(ss.SubjectId, ss.Subject.Name))
                .ToList());
    }
}