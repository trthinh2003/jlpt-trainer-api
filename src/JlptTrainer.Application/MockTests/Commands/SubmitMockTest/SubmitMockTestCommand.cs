using JlptTrainer.Domain.Enums;
using MediatR;

namespace JlptTrainer.Application.MockTests.Commands.SubmitMockTest
{
    public sealed record SubmitMockTestCommand(
        JlptLevel Level,
        List<MockTestAnswer> Answers,
        TimeSpan Duration) : IRequest<SubmitMockTestResult>;
}
