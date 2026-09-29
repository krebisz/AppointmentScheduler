# Doctorly calendar API

Assessment implementation. See `ASSESSMENT.md` for the supplied brief and `WORKLOG.md` for current verified coverage and decisions.

## Prerequisite

.NET SDK 10.0.401.

## Run

    dotnet restore AppointmentScheduler.slnx
    dotnet run --project AppointmentScheduler.csproj --urls http://127.0.0.1:5158

The API creates appointment-scheduler.db in the working directory on first run.

## Create an event

POST /api/events with application/json:

    {
      "title": "Consultation",
      "description": "Annual review",
      "startTime": "2026-10-01T09:00:00Z",
      "endTime": "2026-10-01T09:30:00Z",
      "attendees": [
        {
          "name": "Alex Patient",
          "emailAddress": "alex@example.com",
          "isAttending": false
        }
      ]
    }

A valid request returns HTTP 201 with generated event and attendee IDs. Timestamps are normalized to UTC. The event requires a title, description, a valid time range, and at least one attendee; attendee emails must be valid and unique within the event.

## List, filter, and search

GET /api/events returns events ordered by UTC start time. Optional query parameters can be combined:

- from: includes events whose end time is on or after this timestamp.
- to: includes events whose start time is on or before this timestamp.
- search: case-insensitive title or description search.

The date filters therefore use inclusive overlap semantics. An inverted date range returns HTTP 400.

## Tests

    dotnet test AppointmentScheduler.slnx

Last verified: 8 passed, 0 failed, 0 skipped.

## API contract

In Development, the OpenAPI 3.1.1 document is available at /openapi/v1.json. It was verified to contain /api/events. An interactive documentation UI and generated third-party client are not implemented yet.

## Design and current limits

- Domain owns the Event aggregate, attendees, and invariants.
- Application orchestrates creation through an event-repository port.
- Infrastructure implements that port using EF Core 10.0.12 and SQLite.
- The root API project owns HTTP contracts, controllers, and dependency injection.
- Database creation currently uses EnsureCreated rather than migrations.
- Update, cancel/delete, notifications, accept/reject, and concurrency handling are not implemented yet.
