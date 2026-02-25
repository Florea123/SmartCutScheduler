using FluentValidation;

namespace SmartCutScheduler.Application.Features.Appointments.CreateAppointment;

public class CreateAppointmentCommandValidator : AbstractValidator<CreateAppointmentCommand>
{
    public CreateAppointmentCommandValidator()
    {
        RuleFor(x => x.BarberId).NotEmpty();
        RuleFor(x => x.ServiceId).NotEmpty();
        RuleFor(x => x.AppointmentDate).GreaterThanOrEqualTo(DateTime.Today);
        RuleFor(x => x.StartTime).NotEmpty().Matches(@"^([0-1]?[0-9]|2[0-3]):[0-5][0-9]$")
            .WithMessage("Invalid time format. Use HH:mm.");
    }
}
