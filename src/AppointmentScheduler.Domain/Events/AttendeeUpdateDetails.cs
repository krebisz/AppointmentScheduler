namespace AppointmentScheduler.Domain.Events;

public sealed record AttendeeUpdateDetails(
    string Name,
    string EmailAddress,
    bool? IsAttending = null,
    Guid? AttendeeId = null);
