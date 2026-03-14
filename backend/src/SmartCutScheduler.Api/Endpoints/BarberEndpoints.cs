using MediatR;
using SmartCutScheduler.Application.Features.Barbers.GetAllBarbers;
using SmartCutScheduler.Application.Features.Barbers.GetBarber;
using SmartCutScheduler.Application.Features.Barbers.GetBarberWorkSchedule;
using SmartCutScheduler.Application.Features.Barbers.CreateBarber;
using SmartCutScheduler.Application.Features.Barbers.DeleteBarber;
using SmartCutScheduler.Application.Features.Availability.GetDaySlots;

namespace SmartCutScheduler.Api.Endpoints;

public static class BarberEndpoints
{
    public static void MapBarberEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/barbers").WithTags("Barbers");

        group.MapGet("", async (IMediator mediator) =>
            await mediator.Send(new GetAllBarbersQuery()));

        group.MapPost("", async (HttpRequest request, IMediator mediator, SmartCutScheduler.Application.Common.Interfaces.IFileStorageService fileStorageService) =>
        {
            try
            {
                if (!request.HasFormContentType)
                    return Results.BadRequest(new { message = "Conținut invalid (așteptat multipart/form-data)" });

                var form = await request.ReadFormAsync();
                var name = form["Name"].ToString();
                var email = form["Email"].ToString();
                var phone = form["PhoneNumber"].ToString();
                var password = form["Password"].ToString();
                var description = form["Description"].ToString();
                var file = form.Files["ProfileImage"];
                if (file == null || file.Length == 0)
                    return Results.BadRequest(new { message = "Poza de profil este obligatorie!" });

                // Creează un ID temporar pentru imagine
                var tempId = Guid.NewGuid();
                var imageUrl = await fileStorageService.SaveProfileImageAsync(tempId, file, default);

                var cmd = new CreateBarberCommand(
                    name,
                    email,
                    phone,
                    password,
                    description,
                    imageUrl
                );
                var barberId = await mediator.Send(cmd);
                return Results.Ok(new { id = barberId, message = "Frizer creat cu succes!" });
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
        })
        .RequireAuthorization(policy => policy.RequireRole("Admin"));

        group.MapGet("/{id:guid}", async (Guid id, IMediator mediator) =>
            await mediator.Send(new GetBarberQuery(id)));

        group.MapGet("/{id:guid}/schedule", async (Guid id, IMediator mediator) =>
            await mediator.Send(new GetBarberWorkScheduleQuery(id)));

        group.MapGet("/{id:guid}/test", () => Results.Ok(new { message = "Test endpoint works!" }));

        group.MapGet("/{id:guid}/day-slots", async (Guid id, DateTime date, IMediator mediator) =>
            await mediator.Send(new GetDaySlotsQuery(id, date)));

        group.MapDelete("/{id:guid}", async (Guid id, IMediator mediator) =>
            await mediator.Send(new DeleteBarberCommand(id)))
            .RequireAuthorization(policy => policy.RequireRole("Admin"));
    }
}
