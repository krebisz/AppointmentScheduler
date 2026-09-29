# Doctorly calendar API

Assessment implementation. See `ASSESSMENT.md` for the supplied brief and `WORKLOG.md` for current verified coverage and decisions.

## Download and prerequisites

Download or clone the repository, then open a terminal in the directory containing `AppointmentScheduler.slnx`. Install the .NET 10 SDK. The solution was verified with SDK 10.0.401; the accompanying assessment email explicitly waives the PDF's original .NET 5 requirement.

Confirm the SDK before continuing:

    dotnet --version

No separate database server, migration command, secrets, authentication setup, frontend, or container runtime is required.

## Build, test, and run

From the repository root:

    dotnet restore AppointmentScheduler.slnx
    dotnet build AppointmentScheduler.slnx --no-restore
    dotnet test AppointmentScheduler.slnx --no-build --no-restore
    dotnet run --project AppointmentScheduler.csproj --no-build --launch-profile http

The verified result is a clean build and 22 passing tests. Keep the final command running and wait for:

    Now listening on: http://localhost:5158

The API creates appointment-scheduler.db in the repository root on first run and retains data across restarts. The database and related SQLite files are ignored by Git.

Because this assessment uses `EnsureCreated` rather than migrations, an older local database is not upgraded. If you ran an earlier schema, stop the API and remove `appointment-scheduler.db` once before restarting. A fresh clone needs no database step.

Confirm the API is running with Postman, a browser, or another HTTP client:

    GET http://localhost:5158/api/events

The response is HTTP 200 with an empty JSON array for a new database, or an array of existing active events.

Swagger provides the quickest way to inspect and call every endpoint:

    http://localhost:5158/swagger/index.html

Keep the `dotnet run` terminal open while using Swagger, Postman, or another HTTP client. Stop the API with Ctrl+C.

## Endpoint summary

| Method | Route | Purpose | Success |
| --- | --- | --- | --- |
| GET | `/api/events` | List, filter, and search events | 200 |
| POST | `/api/events` | Create an event | 201 |
| PUT | `/api/events/{id}` | Replace event details and attendees | 200 |
| DELETE | `/api/events/{id}` | Soft-cancel an event | 204 |
| PATCH | `/api/events/{eventId}/attendees/{attendeeId}/attendance` | Accept or reject for one attendee | 204 |

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
- includeCancelled: includes soft-cancelled events when true; defaults to false.

The date filters therefore use inclusive overlap semantics. An inverted date range returns HTTP 400.

Example combined query:

    GET /api/events?from=2026-10-01T00:00:00Z&to=2026-10-31T23:59:59Z&search=consultation&includeCancelled=false

## Update and cancel

- Create, list, and update responses include a numeric `version`.
- PUT /api/events/{id} fully replaces the event details and attendee collection. Include the latest `version` in the request; a stale version returns HTTP 409.
- DELETE /api/events/{id} soft-cancels the event and returns HTTP 204. Repeating the cancellation is idempotent.
- Missing events return HTTP 404. Cancelled events cannot be updated.

Example PUT body using the version returned by create or list:

    {
      "version": 1,
      "title": "Updated consultation",
      "description": "Updated annual review",
      "startTime": "2026-10-01T10:00:00Z",
      "endTime": "2026-10-01T10:30:00Z",
      "attendees": [
        {
          "name": "Alex Patient",
          "emailAddress": "alex@example.com",
          "isAttending": false
        }
      ]
    }

If a write returns HTTP 409, call GET /api/events again, locate the event by ID, and decide whether to retry using the new version. The API does not merge changes automatically.

## Accept or reject an event

An attendee response is represented by the existing `isAttending` boolean. Send the latest event version:

    PATCH /api/events/{eventId}/attendees/{attendeeId}/attendance

    {
      "isAttending": true,
      "version": 1
    }

The endpoint returns HTTP 204. Read the event again to obtain its incremented version. This intentionally models only a simple accept/reject state, not invitation workflow, response history, or comments.

## Notifications

After a create, update, or first cancellation is persisted, the application requests an event notification for the current attendee email addresses. The current infrastructure adapter is intentionally simulated: it writes a structured informational log containing the notification type, event ID, and recipient count, but sends no external email or message and does not log recipient addresses.

Notification publication occurs after persistence. Repeating an idempotent cancellation does not publish another notification. External delivery guarantees, retries, and transactional outbox behavior are not implemented.

## Tests

    dotnet test AppointmentScheduler.slnx

Last verified: 22 passed, 0 failed, 0 skipped.

## API contract

The http launch profile uses the Development environment. Its OpenAPI 3.1.1 document is available at:

    http://localhost:5158/openapi/v1.json

It was verified to contain /api/events. A generated third-party client is not implemented.

Interactive Swagger documentation is available in Development at:

    http://localhost:5158/swagger/index.html

A generated third-party client is intentionally deferred. The OpenAPI JSON remains available as the machine-readable generation input.

## Configuration

The default connection string is in `appsettings.json`:

    Data Source=appointment-scheduler.db

Override it without editing source by setting the standard .NET configuration variable `ConnectionStrings__SchedulerDatabase`. The default HTTP launch profile sets `ASPNETCORE_ENVIRONMENT=Development`, which enables OpenAPI and Swagger UI.

## Troubleshooting

- `ECONNREFUSED` means the API is not listening at that address. Start it with the documented `dotnet run` command and wait for `Now listening on: http://localhost:5158`.
- If port 5158 is already in use, stop the conflicting process or run with `--urls http://localhost:<another-port>` and use that URL in the client.
- If an older local database reports a missing column after a model change, stop the API and remove the ignored `appointment-scheduler.db` file once. A fresh download does not require this.
- Swagger and the OpenAPI document are available only while running in Development.

## Design and current limits

- Domain owns the Event aggregate, attendees, and invariants.
- Application orchestrates lifecycle use cases through repository and channel-neutral notification ports.
- Infrastructure implements those ports using EF Core 10.0.12 with SQLite and an in-process structured-log notification adapter.
- The root API project owns HTTP contracts, controllers, and dependency injection.
- Database creation currently uses EnsureCreated rather than migrations.
- Same-event writes use a small optimistic-concurrency mechanism: clients send the latest numeric version and stale writes return HTTP 409; EF Core also checks the version while saving.
- Concurrency handling does not attempt automatic retries, merging, ETags, distributed coordination, or conflict history.
- Attendance is a single boolean per attendee; richer invitation workflows are outside the assessment scope.
- Swagger UI is Development-only. Hosting public production documentation and generating/distributing a client package are deferred.
- External notification delivery and retries/outbox behavior are not implemented.
