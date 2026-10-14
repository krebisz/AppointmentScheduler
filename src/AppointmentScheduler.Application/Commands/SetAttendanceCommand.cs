namespace AppointmentScheduler.Application.Commands;

public sealed record SetAttendanceCommand(
    Guid EventId,
    Guid AttendeeId,
    bool IsAttending,
    long ExpectedVersion);
