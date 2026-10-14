namespace AppointmentScheduler.Application.Queries;

public sealed record ListEventsQuery(
    DateTimeOffset? From,
    DateTimeOffset? To,
    string? Search,
    bool IncludeCancelled);
