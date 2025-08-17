using FluentValidation;
using Users.Application.Dtos;

namespace Users.Application.Dtos;

public class PreferenceCreateDtoValidator : AbstractValidator<PreferenceCreateDto>
{
    public PreferenceCreateDtoValidator()
    {
        RuleFor(x => x.UserId).GreaterThan(0);
        RuleFor(x => x.WcagVersion).NotEmpty().Must(v => v == "2.0" || v == "2.1" || v == "2.2");
        RuleFor(x => x.WcagLevel).NotEmpty().Must(l => l == "A" || l == "AA" || l == "AAA");
        RuleFor(x => x.FontSize).GreaterThanOrEqualTo(8).When(x => x.FontSize.HasValue);
    }
}