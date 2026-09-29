namespace AppointmentScheduler.Application.Events.Cancel;

public sealed class CancelEventHandler(IEventRepository eventRepository)
{
    public async Task HandleAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var calendarEvent = await eventRepository.GetByIdAsync(
            id,
            cancellationToken)
            ?? throw new EventNotFoundException(id);

        calendarEvent.Cancel();
        await eventRepository.SaveChangesAsync(cancellationToken);
    }
}
