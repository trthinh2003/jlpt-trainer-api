using JlptTrainer.Application.Common.Interfaces;
using MediatR;

namespace JlptTrainer.Application.Vocabs.Queries.LookupWord
{
    public sealed class LookupWordQueryHandler(IWordLookupService wordLookupService) : IRequestHandler<LookupWordQuery, List<LookupWordResultDto>>
    {
        public async Task<List<LookupWordResultDto>> Handle(LookupWordQuery request, CancellationToken cancellationToken)
        {
            var results = await wordLookupService.SearchAsync(request.Keyword, cancellationToken);

            return results
                .Select(r => new LookupWordResultDto(r.Word, r.Reading, r.Meanings, r.IsCommon))
                .ToList();
        }
    }
}
