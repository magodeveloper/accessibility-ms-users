using FluentValidation;
using Users.Application.Dtos;

namespace Users.Application.Dtos
{
    public class UserCreateDtoValidator : AbstractValidator<UserCreateDto>
    {
        public UserCreateDtoValidator()
        {
            RuleFor(x => x.Nickname).NotEmpty().Length(3, 15);
            RuleFor(x => x.Name).NotEmpty().Length(2, 30);
            RuleFor(x => x.Lastname).NotEmpty().Length(2, 30);
            RuleFor(x => x.Email).NotEmpty().EmailAddress();
            RuleFor(x => x.Password).NotEmpty().MinimumLength(8);
        }
    }
}