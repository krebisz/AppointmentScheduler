# Implementation guide

This guide explains where the current behavior lives and how to change it. For initial setup, HTTP examples, Swagger URLs, and troubleshooting, start with [`README.md`](README.md).

## Starting points

- `AppointmentScheduler.slnx`: all production and test projects.
- `src/AppointmentScheduler.Api/Program.cs`: application entry point, DI composition, middleware order, OpenAPI, and database creation.
- `src/AppointmentScheduler.Api/Controllers/EventsController.cs`: all public event routes and transport/application mapping.
- `src/AppointmentScheduler.Api/Events/`: request/response contracts and data-annotation validation.
- `src/AppointmentScheduler.Api/ErrorHandling/ApiExceptionHandler.cs`: centralized exception-to-Problem-Details mapping.
- `src/AppointmentScheduler.Domain/Events/CalendarEvent.cs`: aggregate rules and state transitions.
- `src/AppointmentScheduler.Application/Events/*`: use-case handlers, commands/results, and application ports.
- `src/AppointmentScheduler.Infrastructure/DependencyInjection.cs`: infrastructure registrations.
- `src/AppointmentScheduler.Infrastructure/Persistence/*`: EF model and repository.

Public HTTP operations:

| Method and route | Application entry point |
| --- | --- |
| `POST /api/events` | `CreateEventHandler.HandleAsync` |
| `GET /api/events` | `ListEventsHandler.HandleAsync` |
| `PUT /api/events/{id}` | `UpdateEventHandler.HandleAsync` |
| `DELETE /api/events/{id}` | `CancelEventHandler.HandleAsync` |
| `PATCH /api/events/{eventId}/attendees/{attendeeId}/attendance` | `SetAttendanceHandler.HandleAsync` |

There are no scheduled processes, background workers, domain-event dispatcher, authentication endpoints, or UI entry points.

## Key workflows

### Create

Follow this path:

1. `CreateEventRequest` defines HTTP validation.
2. `EventsController.Create` rejects null attendee entries and maps to `CreateEventCommand`.
3. `CreateEventHandler.HandleAsync` calls `CalendarEvent.Create`.
4. `CalendarEvent.ValidateDetails` and `Attendee.Create` enforce invariant rules and normalize UTC/text.
5. `IEventRepository.AddAsync` is implemented by `EfEventRepository`, which saves the full aggregate.
6. `IEventNotificationPublisher.PublishAsync` runs only after the save.
7. The controller maps `CreateEventResult` to a 201 response.

Start with `CreateEventEndpointTests` for the HTTP path, `CreateEventHandlerTests` for SQLite persistence, and `CalendarEventTests` for invariant examples.

### List, filter, and search

`EventsController.List` passes raw query values to `ListEventsHandler`. The handler normalizes dates/search and rejects an inverted range. `EfEventRepository.ListAsync` builds one no-tracking query, applies active/date/search filters, and orders by UTC start then ID. `ListEventsEndpointTests` demonstrates overlap and search semantics.

To change filter meaning, update all three relevant points: `ListEventsQuery`/handler normalization, `EventQuery`/repository translation, and endpoint tests. Search currently treats SQLite `LIKE` wildcard characters as wildcards.

### Update

`EventsController.Update` maps a full replacement request. `UpdateEventHandler` loads the aggregate, returns 404 through `EventNotFoundException` when absent, checks the supplied version, then calls `CalendarEvent.Update`. Update validates the full replacement, creates a new attendee collection with new attendee IDs, increments the version, saves, publishes `Updated`, and returns the new state.

Both the application version comparison and EF concurrency token are required: the first identifies an already-stale client; the second detects a race after loading.

### Cancel

`CancelEventHandler` loads the aggregate and calls `CalendarEvent.Cancel`. The first transition increments the version, saves, and publishes `Cancelled`. Later DELETE calls return successfully without another save or notification. DELETE has no client-supplied version; EF still detects a race during a state-changing save.

### Attendance response

`SetAttendanceRequest` requires an explicit nullable boolean value and positive version so an omitted value cannot be interpreted as `false`. `SetAttendanceHandler` performs event lookup and version checking, then `CalendarEvent.SetAttendance` checks event state and attendee membership. A changed value saves, increments the event version, and publishes `Updated`; an identical value returns 204 without a write.

### Failure path

ASP.NET Core model validation handles binding, malformed JSON, required values, formats, lengths, and collection minimums. Controller checks cover null attendee elements. Application/domain exceptions propagate to `ApiExceptionHandler`, which returns 400/404/409. Other exceptions are logged and returned as safe 500 responses with a trace ID. EF concurrency failures are translated in `EfEventRepository.SaveChangesAsync`.

## Business rules and enforcement

| Rule | Enforcement |
| --- | --- |
| Required/limited event and attendee text | API data annotations and Domain `RequiredText` |
| Valid attendee email | API `EmailAddress` plus Domain `MailAddress.TryCreate` |
| At least one non-null attendee | API `Required`/`MinLength`, controller null-entry check, Domain count/null check |
| Unique attendee emails per event | Domain case-insensitive grouping; database unique composite index provides an additional provider-collation constraint |
| End later than start | Domain |
| UTC normalization | Domain for writes; list handler for filters; EF UTC-ticks converter |
| Cancelled event cannot update/alter attendance | Domain |
| Cancellation idempotent | Domain boolean result and application early return |
| Update/attendance stale version rejected | Application comparison and EF concurrency token |
| Notifications follow successful persistence | Application handler call order; covered by tests |
| Cancelled events hidden by default | Repository list predicate |
| Inclusive date overlap | Repository predicates |

No rule prevents overlapping appointments or checks attendee availability.

## Persistence details

`SchedulerDbContext.OnModelCreating` is the authoritative model definition. Event lookup is tracked and includes attendees because mutation follows. Listing is no-tracking. The owned attendee collection is persisted in a separate `Attendees` table and follows the event lifecycle.

Writes use one scoped context and one EF `SaveChanges` call; EF supplies its normal transaction for multi-row changes. Notification publication occurs after that database transaction and is not atomic with it.

The version column participates in EF update predicates. `DbUpdateConcurrencyException` becomes a 409-facing application exception. Update replaces all attendees rather than matching existing attendees, so attendee IDs change on every update.

Startup calls `EnsureCreatedAsync`. There are no migrations or seed records. The default SQLite file persists across runs but must be replaced when the model changes. Backward-compatible schema upgrades are explicitly outside the current scope.

## Security and failure behaviour

All endpoints are anonymous. `UseAuthorization` alone does not enforce access; no authentication service, policies, roles, ownership checks, or `[Authorize]` metadata exist. Treat the API as local/demo software rather than a protected patient-data service.

Expected HTTP failures are documented in [`README.md`](README.md#error-responses). A notable boundary is notification failure: because publication follows the commit, the API can return 500 after the event was saved. Callers should inspect current state before retrying a write.

## Configuration and local execution

Required prerequisite: .NET 10 SDK (verified with 10.0.401). The default connection is `ConnectionStrings:SchedulerDatabase` in `appsettings.json`; override it with `ConnectionStrings__SchedulerDatabase`. No secrets are required.

From the repository root:

```powershell
dotnet restore AppointmentScheduler.slnx
dotnet build AppointmentScheduler.slnx --no-restore
dotnet test AppointmentScheduler.slnx --no-build --no-restore
dotnet run --project src/AppointmentScheduler.Api/AppointmentScheduler.Api.csproj --no-build --launch-profile http
```

Development listens at `http://localhost:5158`; Swagger is `/swagger/index.html` and OpenAPI JSON is `/openapi/v1.json`. See the README for request examples and database reset instructions.

## Extension and debugging map

| Change or symptom | Primary location | Tests to run/check |
| --- | --- | --- |
| Add/change an event invariant | `CalendarEvent.cs` | Domain tests, then affected API lifecycle tests |
| Add an operation | new Application handler, controller action/contract, repository method if needed | focused application test plus API test |
| Change filters/search/order | `ListEventsHandler`, `EventQuery`, `EfEventRepository.ListAsync` | `ListEventsEndpointTests` against SQLite |
| Change EF shape/index/conversion | `SchedulerDbContext.OnModelCreating` | infrastructure/API tests using SQLite; add migrations if productionizing |
| Add a notification channel | implement `IEventNotificationPublisher`, register in Infrastructure | application ordering/content tests and adapter integration tests |
| Change error mapping | `ApiExceptionHandler` | `ErrorHandlingTests` in Development and Production modes |
| Diagnose 400 | response `errors`, request contract annotations, controller null-entry checks, Domain validation |
| Diagnose 409 | request version, `UpdateEventHandler`/`SetAttendanceHandler`, EF logs, `EventConcurrencyTests` |
| Diagnose 500 | response trace ID and server log; repository/publisher/configuration are common boundaries |
| Diagnose schema mismatch | configured SQLite path and `EnsureCreated` limitation; use a fresh database |

Keep HTTP concerns in the API, orchestration in Application, invariants in Domain, and external/provider code in Infrastructure. Add an interface only at a boundary that needs substitution; the current design intentionally avoids generic repositories, mediator infrastructure, and speculative services.
