namespace AppointmentScheduler.Application.Events.Create;

public sealed record CreateAttendeeCommand(
    string Name,
    string EmailAddress,
    bool IsAttending);
