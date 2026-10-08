using System.ComponentModel.DataAnnotations;

namespace AppointmentScheduler.Api.Events.Attendance;

public sealed record SetAttendanceRequest(
    [param: Required]
    bool? IsAttending,
    [param: Range(1, long.MaxValue)]
    long Version);
