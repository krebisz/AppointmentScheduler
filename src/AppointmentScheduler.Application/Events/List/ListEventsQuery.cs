namespace AppointmentScheduler.Application.Events.List;

public sealed record ListEventsQuery(
    DateTimeOffset? From,
    DateTimeOffset? To,
    string? Search,
    bool IncludeCancelled);
