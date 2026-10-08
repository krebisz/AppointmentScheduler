# Doctorly calendar API

Assessment implementation for a doctor's-practice calendar API.

## Reviewer guide

This README is the entry point for building, running, using, and reviewing the solution. The supporting documents have distinct purposes:

- [`NET Technical Test.pdf`](<NET Technical Test.pdf>) is the original assessment brief.
- [`ASSESSMENT.md`](ASSESSMENT.md) is a searchable transcription of the brief and its email clarification; it contains requirements, not implementation claims.
- [`WORKLOG.md`](WORKLOG.md) records prioritisation, assumptions, architectural decisions, verified results, deferred scope, and phase commit hashes.

For a short technical review, run the commands below, open Swagger, then follow the create-event vertical slice described under **Architecture and repository structure**. `git log --oneline --reverse` shows the implementation sequence; the phase table in `WORKLOG.md` explains what each checkpoint delivered.

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
    dotnet run --project src/AppointmentScheduler.Api/AppointmentScheduler.Api.csproj --no-build --launch-profile http

The verified result is a clean build and 56 passing tests. Keep the final command running and wait for:

    Now listening on: http://localhost:5158

The API creates appointment-scheduler.db in the repository root on first run and retains data across restarts. The database and related SQLite files are ignored by Git.

Because this assessment uses `EnsureCreated` rather than migrations, an older local database is not upgraded. Backward compatibility is intentionally out of scope. If you ran an earlier schema, stop the API, back up any data you need, and remove the old database before restarting. Resetting the database discards its stored events. Alternatively, point the connection string at a new database file to retain the original untouched. A fresh clone needs no database step.

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

The endpoint requires an explicit true or false response and a positive version; omitted or null attendance values return 400. It returns HTTP 204 on success. Read the event again to obtain the current version (unchanged if the attendance value was already the same). This intentionally models only a simple accept/reject state, not invitation workflow, response history, or comments.

## Notifications

After a create, update, or first cancellation is persisted, the application requests an event notification for the current attendee email addresses. The current infrastructure adapter is intentionally simulated: it writes a structured informational log containing the notification type, event ID, and recipient count, but sends no external email or message and does not log recipient addresses.

Notification publication occurs after persistence. Repeating an idempotent cancellation does not publish another notification. External delivery guarantees, retries, and transactional outbox behavior are not implemented.

If publication throws after persistence, the API returns 500 but the saved event remains. A failed write response does not guarantee rollback: inspect stored data before retrying, especially after POST, which could otherwise create a duplicate.

## Error responses

Request errors use JSON `application/problem+json`. ASP.NET validation handles missing bodies/fields and malformed JSON; collection checks reject null attendees. One API exception handler maps domain/application exceptions and unexpected failures consistently:

- 400: invalid inputs, date ranges, or domain rules (including unknown attendee membership).
- 404: event or route not found.
- 409: stale version or a conflicting database save.
- 500: unexpected storage, application, or notification failure. The response is generic with a trace ID; details and the exception are logged server-side in both Development and Production.

Infrastructure translates EF concurrency errors into the application conflict exception. Other failures propagate to the API handler; failed persistence prevents notification publication. Startup/database creation failures stop startup rather than exposing a running, unusable API.

## Tests

    dotnet test AppointmentScheduler.slnx

Last verified: 56 passed, 0 failed, 0 skipped. Includes null/malformed inputs, expected errors, injected repository and save failures, post-save notification failure, and safe Production error responses.

### Test methodology

Tests are placed at the boundary where each risk is most useful to detect:

- Domain tests exercise aggregate invariants and state transitions without framework setup.
- Application tests use small fakes to verify orchestration, notification content, and save-before-publish ordering.
- Infrastructure tests use SQLite rather than EF's in-memory provider so relational mapping, queries, constraints, and optimistic concurrency use the same provider as the application.
- API tests use `WebApplicationFactory` to exercise routing, model validation, serialization, centralized error mapping, dependency injection, and persistence through HTTP.

This mix keeps business-rule tests fast while retaining focused integration coverage for behavior that unit tests cannot prove. Failure-path tests inject repository, save, and notification failures to verify propagation and the public `ProblemDetails` contract.

## API contract

The http launch profile uses the Development environment. Its OpenAPI 3.1.1 document is available at:

    http://localhost:5158/openapi/v1.json

It was verified to contain /api/events. A generated third-party client is not implemented.

Interactive Swagger documentation is available in Development at:

    http://localhost:5158/swagger/index.html

A generated third-party client is intentionally deferred. The OpenAPI JSON remains available as the machine-readable generation input.

## Configuration

The default connection string is in `appsettings.json`:

    Data Source=../../appointment-scheduler.db

The relative path is resolved from the API project directory during `dotnet run`, so the existing repository-root database is retained. For another working directory or deployment, set an explicit database path. Override it without editing source by setting the standard .NET configuration variable `ConnectionStrings__SchedulerDatabase`. The default HTTP launch profile sets `ASPNETCORE_ENVIRONMENT=Development`, which enables OpenAPI and Swagger UI.

## Troubleshooting

- `ECONNREFUSED` means the API is not listening at that address. Start it with the documented `dotnet run` command and wait for `Now listening on: http://localhost:5158`.
- If port 5158 is already in use, stop the conflicting process or run with `--urls http://localhost:<another-port>` and use that URL in the client.
- If an older local database reports a missing column after a model change, stop the API and remove the ignored `appointment-scheduler.db` file once. A fresh download does not require this.
- Swagger and the OpenAPI document are available only while running in Development.

## Design and current limits

- Domain owns the Event aggregate, attendees, and invariants.
- Application orchestrates lifecycle use cases through repository and channel-neutral notification ports.
- Infrastructure implements those ports using EF Core 10.0.12 with SQLite and an in-process structured-log notification adapter.
- `src/AppointmentScheduler.Api` owns HTTP contracts, controllers, and dependency injection.
- Database creation currently uses EnsureCreated rather than migrations.
- Same-event writes use a small optimistic-concurrency mechanism: clients send the latest numeric version and stale writes return HTTP 409; EF Core also checks the version while saving.
- Concurrency handling does not attempt automatic retries, merging, ETags, distributed coordination, or conflict history.
- Attendance is a single boolean per attendee; richer invitation workflows are outside the assessment scope.
- Swagger UI is Development-only. Hosting public production documentation and generating/distributing a client package are deferred.
- External notification delivery and retries/outbox behavior are not implemented.

## Architecture and repository structure

The solution uses four explicit boundaries with inward dependencies:

    API -> Application <- Infrastructure
               |
             Domain

- `src/AppointmentScheduler.Api`: `Program.cs` composes dependencies; `Controllers/` owns routes and response mapping, `Events/<UseCase>/` owns HTTP contracts, and `ErrorHandling/` owns centralized exception mapping.
- `src/AppointmentScheduler.Domain`: the `CalendarEvent` aggregate owns attendees and enforces lifecycle and validation rules. It has no application, persistence, or HTTP dependency.
- `src/AppointmentScheduler.Application`: use-case handlers orchestrate the aggregate through repository and notification interfaces. It depends only on Domain.
- `src/AppointmentScheduler.Infrastructure`: EF Core/SQLite persistence and the simulated logging notification adapter implement Application interfaces.
- `tests/AppointmentScheduler.Tests`: tests are grouped by Domain, Application, Infrastructure, and API to mirror the production boundaries.

The main vertical slice demonstrates the complete dependency path without extra mediator or generic-repository machinery:

    POST /api/events
      -> EventsController and request contract
      -> CreateEventHandler
      -> CalendarEvent.Create and attendee invariants
      -> IEventRepository / EfEventRepository
      -> SchedulerDbContext / SQLite
      -> notification port / structured-log adapter

The key files for that walkthrough are [`EventsController.cs`](src/AppointmentScheduler.Api/Controllers/EventsController.cs), [`CreateEventHandler.cs`](src/AppointmentScheduler.Application/Events/Create/CreateEventHandler.cs), [`CalendarEvent.cs`](src/AppointmentScheduler.Domain/Events/CalendarEvent.cs), [`EfEventRepository.cs`](src/AppointmentScheduler.Infrastructure/Persistence/EfEventRepository.cs), and [`CreateEventEndpointTests.cs`](tests/AppointmentScheduler.Tests/Api/Events/Create/CreateEventEndpointTests.cs).

The same separation supports listing, updating, cancellation, attendance response, and concurrency handling. The detailed reasons and trade-offs, including SQLite, aggregate ownership, soft cancellation, UTC storage, notification timing, optimistic concurrency, and deferred features, are recorded in `WORKLOG.md`.

## Refactor navigation and diagrams

See [REFACTORING.md](REFACTORING.md) for the old-to-new path/namespace map, compatibility evidence, and diagram regeneration instructions. `CodeMap1.dgml` is a stale generated snapshot; use current source as the authority.
