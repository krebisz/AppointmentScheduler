# Implementation guide

This guide explains where the current behavior lives and how to change it. For initial setup, HTTP examples, Swagger URLs, and troubleshooting, start with [`README.md`](README.md).

## Structure rule

Group by artifact within each project, keeping folder namespaces and matching type files. The four projects retain responsibility/dependency boundaries. Operation names connect files across artifact directories; no use-case or Events container is required for current navigation.

## Starting points

- API Program.cs: host, DI, middleware, initialization.
- API Controllers/: HTTP routes and response mapping; Requests/ and Responses/: transport contracts.
- API ErrorHandling/ and OpenApi/: errors and executable metadata.
- Application Handlers/: use-case orchestration; Commands/, Queries/, Inputs/, Results/: boundary messages.
- Application Interfaces/: persistence/notification ports; Exceptions/: application errors; Notifications/: notification payload/enum.
- Domain Entities/: CalendarEvent and Attendee; Inputs/: attendee operation payloads; Exceptions/ and Validation/: domain rules/errors.
- Infrastructure Repositories/: EfEventRepository; Configurations/: CalendarEventConfiguration; DbContexts/: context/design factory.
- Infrastructure DatabaseInitialization/: guarded initializer; Migrations/: initial migration/model snapshot; NotificationPublishers/: logging adapter.
- Tests retain Api/Application/Domain/Infrastructure boundaries, grouping by tested artifact. API Fixtures/ contains AppointmentSchedulerApiFactory and EventApiTestSession.

Public HTTP operations:

| Method and route | Application entry point |
| --- | --- |
| `POST /api/events` | `CreateEventHandler.HandleAsync` |
| `GET /api/events/{id}` | `GetEventHandler.HandleAsync` |
| `GET /api/events` | `ListEventsHandler.HandleAsync` |
| `PUT /api/events/{id}` | `UpdateEventHandler.HandleAsync` |
| `DELETE /api/events/{id}` | `CancelEventHandler.HandleAsync` |
| `PATCH /api/events/{eventId}/attendees/{attendeeId}/attendance` | `SetAttendanceHandler.HandleAsync` |

There are no scheduled processes, background workers, domain-event dispatcher, authentication endpoints, or UI entry points.

## Key workflows

### Create

Follow this path:

1. `CreateEventRequest` defines HTTP validation.
2. `CreateEventController.CreateAsync` rejects null attendee entries and maps to `CreateEventCommand`.
3. `CreateEventHandler.HandleAsync` calls `CalendarEvent.Create`.
4. `CalendarEvent.ValidateDetails` and `Attendee.Create` enforce invariant rules and normalize UTC/text.
5. `IEventRepository.AddAndSaveAsync` is implemented by `EfEventRepository`, which saves the full aggregate.
6. `IEventNotificationPublisher.PublishAsync` runs only after the save.
7. The controller maps `CreateEventResult` to a 201 response with `CreatedAtRoute`/Location pointing at `GetEventController.RouteName`.

Start with `CreateEventEndpointTests` for the HTTP path, `CreateEventHandlerTests` for SQLite persistence, and `CalendarEventTests` for invariant examples.

### Retrieve by ID

`GetEventController.GetAsync` calls `GetEventHandler` with `GetEventQuery`. It returns details, attendees, cancellation state and version; cancelled events remain retrievable, missing IDs return 404. The existing tracked lookup is reused without save/mutation/publication. `GetEventEndpointTests` verifies retrieval and following Location.

### List, filter, and search

`ListEventsController.ListAsync` passes raw query values to `ListEventsHandler`. The handler normalizes dates/search and rejects an inverted range. `EfEventRepository.ListAsync` builds one no-tracking query, applies active/date/search filters, and orders by UTC start then ID. `ListEventsEndpointTests` demonstrates overlap and search semantics.

To change filter meaning, update all three relevant points: `ListEventsQuery`/handler normalization, `EventRepositoryQuery`/repository translation, and endpoint tests. Search currently treats SQLite `LIKE` wildcard characters as wildcards.

### Update

`UpdateEventController.UpdateAsync` maps a full replacement request. `UpdateEventHandler` loads the aggregate, returns 404 through `EventNotFoundException` when absent, checks the supplied version, then calls `CalendarEvent.Update`. Update validates the full replacement, retains supplied existing IDs/instances, creates ID-less additions and removes omitted children, increments the version, saves, publishes `Updated`, and returns the new state.

Omitted/null attendance preserves retained answers and defaults false for additions; explicit values replace attendance. Duplicate/foreign IDs and duplicate emails reject the entire request before mutation. Every valid PUT increments version even for identical values.

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
| Required/limited event and attendee text | API data annotations and Domain `RequiredTextValidator.ValidateAndNormalize` |
| Valid attendee email | API `EmailAddress` plus Domain `MailAddress.TryCreate` |
| At least one non-null attendee | API `Required`/`MinLength`, controller null-entry check, Domain count/null check |
| Unique attendee emails per event | Domain case-insensitive grouping; database unique composite index provides an additional provider-collation constraint |
| Timestamp presence on POST/PUT | API `[property: JsonRequired]`; missing/null/malformed values return 400 |
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

`CalendarEventConfiguration` is the authoritative model definition, applied by `SchedulerDbContext.OnModelCreating`. Event lookup is tracked and includes attendees for mutations and GET by ID; reads never save. Listing is no-tracking. The owned attendee collection is persisted in a separate `Attendees` table and follows the event lifecycle.

Writes use one scoped context. Normal writes use an EF save transaction. Retained email changes stage temporary unique emails then final values in two saves within one explicit transaction, permitting email swaps and rolling back both phases on failure. Notification publication occurs after that database transaction and is not atomic with it.

The version column participates in EF update predicates. `DbUpdateConcurrencyException` becomes a 409-facing application exception. Update matches only supplied IDs, retaining tracked objects/identity; it does not infer identity from email.

Startup calls `SchedulerDatabaseInitializer.InitializeAsync`: fresh/managed databases receive migrations, tables without recorded migrations are refused. The initial migration/model snapshot exist; no seed records or automatic legacy transition exists. Preserve the working legacy database and use a separate fresh path. `SchedulerDbContextDesignFactory` requires an explicit connection variable for CLI operations.

## Security and failure behaviour

All endpoints are anonymous. `UseAuthorization` alone does not enforce access; no authentication service, policies, roles, ownership checks, or `[Authorize]` metadata exist. Treat the API as local/demo software rather than a protected patient-data service.

Expected HTTP failures are documented in [`README.md`](README.md#error-responses). A notable boundary is notification failure: because publication follows the commit, the API can return 500 after the event was saved. Callers should inspect current state before retrying a write.

## Configuration and local execution

Required prerequisite: .NET 10 SDK (verified with 10.0.401). The default connection is `ConnectionStrings:SchedulerDatabase` in `appsettings.json`; override it with `ConnectionStrings__SchedulerDatabase`. No secrets are required.

Use the [README setup and migration commands](README.md#build-test-and-run), including its explicit separate database path. Development listens at `http://localhost:5158`; Swagger is `/swagger/index.html` and OpenAPI JSON is `/openapi/v1.json`.

## Extension and debugging map

| Change or symptom | Primary location | Tests to run/check |
| --- | --- | --- |
| Add/change an event invariant | `CalendarEvent.cs` | Domain tests, then affected API lifecycle tests |
| Add an operation | new Application handler, controller action/contract, repository method if needed | focused application test plus API test |
| Change filters/search/order | `ListEventsHandler`, `EventRepositoryQuery`, `EfEventRepository.ListAsync` | `ListEventsEndpointTests` against SQLite |
| Change EF shape/index/conversion | `SchedulerDbContext.OnModelCreating` | infrastructure/API tests using SQLite; generate/review a migration, build it and test fresh/upgrade copies |
| Add a notification channel | implement `IEventNotificationPublisher`, register in Infrastructure | application ordering/content tests and adapter integration tests |
| Change error mapping | `ApiExceptionHandler` | `ErrorHandlingTests` in Development and Production modes |
| Diagnose 400 | response `errors`, request contract annotations, controller null-entry checks, Domain validation |
| Diagnose 409 | request version, `UpdateEventHandler`/`SetAttendanceHandler`, EF logs, `EfEventRepositoryConcurrencyTests` |
| Diagnose 500 | response trace ID and server log; repository/publisher/configuration are common boundaries |
| Diagnose schema mismatch | configured path/history and initializer refusal; preserve legacy data and select a separate database |

Keep HTTP concerns in the API, orchestration in Application, invariants in Domain, and external/provider code in Infrastructure. Add an interface only at a boundary that needs substitution; the current design intentionally avoids generic repositories, mediator infrastructure, and speculative services.

## Recent focused tests and contract metadata

Timestamp presence/non-mutation: `RequiredTimestampTests`, `InvalidEventWriteTests`. Retrieval/Location: `GetEventEndpointTests`. Stable identity/attendance: `UpdateAttendeeIdentityTests`, `AttendeeReplacementTests`. Relational swap/failure rollback: `EfEventRepositoryReconciliationTests`. Migration/history guard: `SchedulerDatabaseInitializerTests`. API metadata: `EventOpenApiTests`.

Changed contracts have executable metadata in `EventOpenApiOperationTransformer` and `UpdateEventSchemaTransformer`; keep these aligned with future route/contract edits. Shared version contention, notification durability and duplicate POST retries remain unresolved. Full verification: 123 passing tests on 8 October 2026.
