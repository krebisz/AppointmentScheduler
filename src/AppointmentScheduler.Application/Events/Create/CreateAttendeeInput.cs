namespace AppointmentScheduler.Application.Events.Create;

public sealed record CreateAttendeeInput(
    string Name,
    string EmailAddress,
    bool IsAttending);
