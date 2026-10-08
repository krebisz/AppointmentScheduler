namespace AppointmentScheduler.Application.Events.Persistence;

public sealed record EventRepositoryQuery(
    DateTimeOffset? From,
    DateTimeOffset? To,
    string? Search,
    bool IncludeCancelled);
