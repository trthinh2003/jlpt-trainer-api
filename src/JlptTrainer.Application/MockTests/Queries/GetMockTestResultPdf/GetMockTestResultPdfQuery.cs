using MediatR;

namespace JlptTrainer.Application.MockTests.Queries.GetMockTestResultPdf
{
    public sealed record GetMockTestResultPdfQuery(Guid MockTestId) : IRequest<byte[]>;
}
