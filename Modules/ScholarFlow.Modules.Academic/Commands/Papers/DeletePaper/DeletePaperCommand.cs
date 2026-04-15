using MediatR;

namespace ScholarFlow.Modules.Academic.Commands.Papers.DeletePaper;

public sealed record DeletePaperCommand(Guid Id) : IRequest;
