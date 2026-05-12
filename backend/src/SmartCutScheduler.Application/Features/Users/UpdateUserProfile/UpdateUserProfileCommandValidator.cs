using FluentValidation;
using Microsoft.AspNetCore.Http;

namespace SmartCutScheduler.Application.Features.Users.UpdateUserProfile;

public class UpdateUserProfileCommandValidator : AbstractValidator<UpdateUserProfileCommand>
{
    public UpdateUserProfileCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Numele este obligatoriu.")
            .MaximumLength(100);
        RuleFor(x => x.PhoneNumber)
            .MaximumLength(20);
        RuleFor(x => x.Description)
            .MaximumLength(500);
        RuleFor(x => x.ProfileImage)
            .Must(BeAValidImage).When(x => x.ProfileImage != null)
            .WithMessage("Fișierul trebuie să fie o imagine validă (jpg, jpeg, png) și max 2MB.");
    }

    private static bool BeAValidImage(IFormFile? file)
    {
        if (file == null) return true;
        var allowed = new[] { ".jpg", ".jpeg", ".png" };
        var ext = System.IO.Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowed.Contains(ext)) return false;
        if (file.Length > 2 * 1024 * 1024) return false; // 2MB
        return true;
    }
}