using AppointmentScheduler.Domain.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace AppointmentScheduler.Infrastructure.Persistence;

public sealed class SchedulerDbContext(DbContextOptions<SchedulerDbContext> options)
    : DbContext(options)
{
    public DbSet<CalendarEvent> Events => Set<CalendarEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var utcTicksConverter = new ValueConverter<DateTimeOffset, long>(
            value => value.UtcTicks,
            value => new DateTimeOffset(value, TimeSpan.Zero));

        var eventBuilder = modelBuilder.Entity<CalendarEvent>();

        eventBuilder.ToTable("Events");
        eventBuilder.HasKey(calendarEvent => calendarEvent.Id);
        eventBuilder.Property(calendarEvent => calendarEvent.Id).ValueGeneratedNever();
        eventBuilder.Property(calendarEvent => calendarEvent.Title)
            .HasMaxLength(CalendarEvent.TitleMaxLength)
            .IsRequired();
        eventBuilder.Property(calendarEvent => calendarEvent.Description)
            .HasMaxLength(CalendarEvent.DescriptionMaxLength)
            .IsRequired();
        eventBuilder.Property(calendarEvent => calendarEvent.StartTime)
            .HasConversion(utcTicksConverter)
            .IsRequired();
        eventBuilder.Property(calendarEvent => calendarEvent.EndTime)
            .HasConversion(utcTicksConverter)
            .IsRequired();

        eventBuilder.OwnsMany(
            calendarEvent => calendarEvent.Attendees,
            attendeeBuilder =>
            {
                attendeeBuilder.ToTable("Attendees");
                attendeeBuilder.WithOwner().HasForeignKey("EventId");
                attendeeBuilder.HasKey(attendee => attendee.Id);
                attendeeBuilder.Property(attendee => attendee.Id).ValueGeneratedNever();
                attendeeBuilder.Property(attendee => attendee.Name)
                    .HasMaxLength(CalendarEvent.AttendeeNameMaxLength)
                    .IsRequired();
                attendeeBuilder.Property(attendee => attendee.EmailAddress)
                    .HasMaxLength(CalendarEvent.EmailAddressMaxLength)
                    .IsRequired();
                attendeeBuilder.Property(attendee => attendee.IsAttending).IsRequired();
                attendeeBuilder.HasIndex("EventId", nameof(Attendee.EmailAddress))
                    .IsUnique();
            });

        eventBuilder.Navigation(calendarEvent => calendarEvent.Attendees)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
