namespace AppointmentScheduler.Application.Events.Attendance;

public sealed record SetAttendanceCommand(
    Guid EventId,
    Guid AttendeeId,
    bool IsAttending,
    long ExpectedVersion);
