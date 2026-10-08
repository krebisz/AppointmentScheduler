namespace AppointmentScheduler.Domain.Events;

public sealed record AttendeeDetails(
    string Name,
    string EmailAddress,
    bool IsAttending);
