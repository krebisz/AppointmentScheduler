using AppointmentScheduler.Domain.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace AppointmentScheduler.Infrastructure.Events.Persistence;

internal sealed class CalendarEventConfiguration : IEntityTypeConfiguration<CalendarEvent>
{
    public void Configure(EntityTypeBuilder<CalendarEvent> builder)
    {
        var utcTicksConverter = new ValueConverter<DateTimeOffset, long>(
            value => value.UtcTicks,
            value => new DateTimeOffset(value, TimeSpan.Zero));

        var eventBuilder = builder;

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
        eventBuilder.Property(calendarEvent => calendarEvent.IsCancelled)
            .IsRequired();
        eventBuilder.Property(calendarEvent => calendarEvent.Version)
            .IsConcurrencyToken()
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
