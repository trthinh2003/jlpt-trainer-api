using MediatR;

namespace JlptTrainer.Application.GrammarPoints.Commands.DeleteGrammarPoint
{
    public sealed record DeleteGrammarPointCommand(Guid Id) : IRequest;
}
