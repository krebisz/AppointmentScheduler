namespace AppointmentScheduler.Application.Inputs;

public sealed record UpdateAttendeeInput(
    string Name,
    string EmailAddress,
    bool? IsAttending = null,
    Guid? AttendeeId = null);
