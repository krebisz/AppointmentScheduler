namespace AppointmentScheduler.Application.Inputs;

public sealed record CreateAttendeeInput(
    string Name,
    string EmailAddress,
    bool IsAttending);
