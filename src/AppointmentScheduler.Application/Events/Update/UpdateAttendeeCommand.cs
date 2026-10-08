namespace AppointmentScheduler.Application.Events.Update;

public sealed record UpdateAttendeeCommand(
    string Name,
    string EmailAddress,
    bool IsAttending);
