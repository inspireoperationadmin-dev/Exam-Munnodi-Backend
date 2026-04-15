using MediatR;
using ScholarFlow.Modules.Academic.DTOs;

namespace ScholarFlow.Modules.Academic.Queries.GetAllStreams;

public sealed record GetAllStreamsQuery : IRequest<List<StreamDto>>;
