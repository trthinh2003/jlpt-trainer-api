using FluentValidation;

namespace JlptTrainer.Application.Kanjis.Commands.CreateKanji
{
    public sealed class CreateKanjiCommandValidator : AbstractValidator<CreateKanjiCommand>
    {
        public CreateKanjiCommandValidator()
        {
            RuleFor(x => x.Character).NotEmpty().MaximumLength(10);
            RuleFor(x => x.Meaning).NotEmpty().MaximumLength(500);
            RuleFor(x => x.StrokeCount).GreaterThan(0).WithMessage("Số nét phải lớn hơn 0.");
            RuleFor(x => x.Level).IsInEnum();         
            
            RuleFor(x => x.OnYomi).MaximumLength(200);//.NotEmpty();
            RuleFor(x => x.KunYomi).MaximumLength(200);//.NotEmpty();
    // (OnYomi/KunYomi có thể để trống (1 số kanji chỉ có 1 trong 2 cách đọc), nên không NotEmpty(), chỉ giới hạn độ dài.)
        }
    }

}
