using MediatR;
using ScholarFlow.Modules.UserProfiles.DTOs;

namespace ScholarFlow.Modules.UserProfiles.Queries.GetStudentProfile;

public sealed record GetStudentProfileQuery : IRequest<StudentProfileDto>;