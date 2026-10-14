namespace AppointmentScheduler.Api.Responses;

public sealed record ListEventItemResponse(
    Guid Id,
    string Title,
    string Description,
    DateTimeOffset StartTime,
    DateTimeOffset EndTime,
    bool IsCancelled,
    long Version,
    IReadOnlyCollection<ListAttendeeResponse> Attendees);
