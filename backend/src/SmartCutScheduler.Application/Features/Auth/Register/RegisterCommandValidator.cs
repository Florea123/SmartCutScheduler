using FluentValidation;
using SmartCutScheduler.Domain.Repositories;

namespace SmartCutScheduler.Application.Features.Auth.Register;

public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator(IUserRepository userRepository)
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100);
        
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MustAsync(async (email, ct) =>
            {
                if (string.IsNullOrWhiteSpace(email)) return true;
                var existing = await userRepository.GetByEmailAsync(email, ct);
                return existing == null;
            })
            .WithMessage("Email already registered.");
        
        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(8)
            .Matches("[A-Z]").WithMessage("Must contain an uppercase letter.")
            .Matches("[a-z]").WithMessage("Must contain a lowercase letter.")
            .Matches("[0-9]").WithMessage("Must contain a digit.")
            .Matches("[^a-zA-Z0-9]").WithMessage("Must contain a non-alphanumeric.");
        
        RuleFor(x => x.PhoneNumber)
            .MaximumLength(20)
            .When(x => !string.IsNullOrWhiteSpace(x.PhoneNumber));
    }
}
