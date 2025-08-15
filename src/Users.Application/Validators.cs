using FluentValidation;

namespace Users.Application;

public class UserCreateDtoValidator : AbstractValidator<UserCreateDto>
{
    public UserCreateDtoValidator()
    {
        RuleFor(x => x.Nickname).NotEmpty().Length(3, 15);
        RuleFor(x => x.Name).NotEmpty().Length(2, 30);
        RuleFor(x => x.Lastname).NotEmpty().Length(2, 30);
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(6);
    }
}

public class LoginDtoValidator : AbstractValidator<LoginDto>
{
    public LoginDtoValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
    }
}

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
