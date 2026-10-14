namespace AppointmentScheduler.Application.Queries;

public sealed record EventRepositoryQuery(
    DateTimeOffset? From,
    DateTimeOffset? To,
    string? Search,
    bool IncludeCancelled);
