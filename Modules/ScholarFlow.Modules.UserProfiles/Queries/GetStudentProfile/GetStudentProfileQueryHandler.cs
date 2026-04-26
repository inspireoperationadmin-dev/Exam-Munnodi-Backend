using MediatR;
using ScholarFlow.Domain.Interfaces;
using ScholarFlow.Domain.Interfaces.Repositories;
using ScholarFlow.Modules.UserProfiles.DTOs;
using ScholarFlow.SharedKernel.Exceptions;

namespace ScholarFlow.Modules.UserProfiles.Queries.GetStudentProfile;

public sealed class GetStudentProfileQueryHandler(
    IStudentProfileRepository profileRepo,
    ICurrentUser              currentUser)
    : IRequestHandler<GetStudentProfileQuery, StudentProfileDto>
{
    public async Task<StudentProfileDto> Handle(
        GetStudentProfileQuery request, CancellationToken ct)
    {
        var profile = await profileRepo.GetByUserIdWithDetailsAsync(currentUser.UserId, ct)
            ?? throw new NotFoundException("Student profile not found.");

        var isSetupComplete =
            profile.AcademicStreamId.HasValue &&
            profile.Medium.HasValue &&
            profile.ExamYear.HasValue &&
            profile.SubjectSelections.Count == 3;

        return new StudentProfileDto(
            Id:              profile.Id,
            UserId:          profile.UserId,
            FullName:        profile.FullName,
            StreamName:      profile.AcademicStream?.Name,
            Medium:          profile.Medium?.ToString(),
            ExamYear:        profile.ExamYear,
            IsSetupComplete: isSetupComplete,
            Subjects: profile.SubjectSelections
                .Select(ss => new StudentSubjectDto(ss.SubjectId, ss.Subject.Name))
                .ToList());
    }
}