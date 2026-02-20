using SmartCutScheduler.Api.Data;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace SmartCutScheduler.Api.Features.Auth.Register;

public class RegisterUserValidator : AbstractValidator<RegisterUserCommand>
{
    public RegisterUserValidator(AppDbContext db)
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MustAsync(async (email, ct) =>
            {
                if (string.IsNullOrWhiteSpace(email)) return true;
                var normalized = email.ToLowerInvariant();
                return !await db.Users.AsNoTracking()
                    .AnyAsync(u => u.Email.ToLower() == normalized, ct);
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
