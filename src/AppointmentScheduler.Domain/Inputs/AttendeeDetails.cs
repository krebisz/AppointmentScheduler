namespace AppointmentScheduler.Domain.Inputs;

public sealed record AttendeeDetails(
    string Name,
    string EmailAddress,
    bool IsAttending);
