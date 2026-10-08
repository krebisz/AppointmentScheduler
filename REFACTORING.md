# Organisation refactor

Verified 8 October 2026 with SDK 10.0.401. Production projects live under `src/`; tests live under `tests/`. Project names, assembly names and root namespaces match. Folder names extend the root namespace. `Program.cs` deliberately retains top-level statements and its global partial `Program` declaration for test hosting.

## Navigation map

Paths below are relative to the repository. Each named type has its own matching `.cs` file.

| Before | After | Namespace |
| --- | --- | --- |
| Root `AppointmentScheduler.csproj` | `src/AppointmentScheduler.Api/AppointmentScheduler.Api.csproj` | Root/assembly: `AppointmentScheduler.Api` |
| Root `Program.cs`, appsettings and launch settings | Same filenames inside `src/AppointmentScheduler.Api/` | `Program` remains global |
| `Controllers/EventsController.cs` | `src/AppointmentScheduler.Api/Controllers/EventsController.cs` | `AppointmentScheduler.Controllers` → `AppointmentScheduler.Api.Controllers` |
| `Controllers/ApiExceptionHandler.cs` | `src/AppointmentScheduler.Api/ErrorHandling/ApiExceptionHandler.cs` | `AppointmentScheduler.Controllers` → `AppointmentScheduler.Api.ErrorHandling` |
| `Controllers/CreateEventContracts.cs` create types | API `Events/Create/{CreateEventRequest,CreateAttendeeRequest,CreateEventResponse,CreatedAttendeeResponse}.cs` | `AppointmentScheduler.Api.Events.Create` |
| Same mixed file, update types | API `Events/Update/{UpdateEventRequest,UpdateAttendeeRequest,UpdateEventResponse,UpdatedAttendeeResponse}.cs` | `AppointmentScheduler.Api.Events.Update` |
| Same mixed file, list types | API `Events/List/{EventListItemResponse,EventAttendeeResponse}.cs` | `AppointmentScheduler.Api.Events.List` |
| Same mixed file, attendance type | API `Events/Attendance/SetAttendanceRequest.cs` | `AppointmentScheduler.Api.Events.Attendance` |
| Application `Events/Create/CreateEventHandler.cs`, including commands/results | Separate handler, `CreateEventCommand`, `CreateAttendeeCommand`, `CreateEventResult`, `CreatedAttendeeResult` files in the same folder | Unchanged: `AppointmentScheduler.Application.Events.Create` |
| Application `Events/Update/UpdateEventHandler.cs`, including commands/results | Separate handler, `UpdateEventCommand`, `UpdateAttendeeCommand`, `UpdateEventResult`, `UpdatedAttendeeResult` files in the same folder | Unchanged: `AppointmentScheduler.Application.Events.Update` |
| Application `Events/List/ListEventsHandler.cs`, including query/results/exception | Separate handler, `ListEventsQuery`, `ListedEventResult`, `ListedAttendeeResult`, `InvalidEventQueryException` files in the same folder | Unchanged: `AppointmentScheduler.Application.Events.List` |
| Application `Events/Attendance/SetAttendanceHandler.cs`, including command | Separate `SetAttendanceHandler.cs` and `SetAttendanceCommand.cs` | Unchanged: `AppointmentScheduler.Application.Events.Attendance` |
| Application `Events/IEventRepository.cs`, including `EventQuery` | `Events/Persistence/IEventRepository.cs` and `EventQuery.cs` | `AppointmentScheduler.Application.Events` → `.Events.Persistence` |
| Application `Events/Notifications/IEventNotificationPublisher.cs`, including message/enum | Separate `IEventNotificationPublisher.cs`, `EventNotification.cs`, `EventNotificationType.cs` | Unchanged: `AppointmentScheduler.Application.Events.Notifications` |
| Domain `Events/CalendarEvent.cs`, including attendee types | Separate `CalendarEvent.cs`, `Attendee.cs`, `AttendeeDetails.cs` | Unchanged: `AppointmentScheduler.Domain.Events` |
| Tests `Api/CreateEventEndpointTests.cs`, including factory | `Api/Events/Create/CreateEventEndpointTests.cs` and shared `Api/ApiFactory.cs` | `AppointmentScheduler.Tests.Api.Events.Create`; factory remains `.Api` |
| Tests `Api/ListEventsEndpointTests.cs` | `Api/Events/List/ListEventsEndpointTests.cs` | `AppointmentScheduler.Tests.Api.Events.List` |
| Tests `Api/EventLifecycleEndpointTests.cs` | `Api/Events/EventLifecycleEndpointTests.cs` | `AppointmentScheduler.Tests.Api.Events` |
| Tests `Api/ErrorHandlingTests.cs` | `Api/ErrorHandling/ErrorHandlingTests.cs` | `AppointmentScheduler.Tests.Api.ErrorHandling` |
| Tests `Application/CreateEventHandlerTests.cs` | `Application/Events/Create/CreateEventHandlerTests.cs` | `AppointmentScheduler.Tests.Application.Events.Create` |
| Tests `Application/EventNotificationTests.cs` | `Application/Events/Notifications/EventNotificationTests.cs` | `AppointmentScheduler.Tests.Application.Events.Notifications` |
| Tests `Domain/CalendarEventTests.cs` | `Domain/Events/CalendarEventTests.cs` | `AppointmentScheduler.Tests.Domain.Events` |
| Tests `Infrastructure/EventConcurrencyTests.cs` | `Infrastructure/Persistence/EventConcurrencyTests.cs` | `AppointmentScheduler.Tests.Infrastructure.Persistence` |
| Tests `Infrastructure/LoggingEventNotificationPublisherTests.cs` | `Infrastructure/Notifications/LoggingEventNotificationPublisherTests.cs` | `AppointmentScheduler.Tests.Infrastructure.Notifications` |

API cancellation has no body/response contract, so no empty `Events/Cancel` folder was added. The Application cancel handler remains in `Events/Cancel`. Cross-operation lifecycle tests remain together under their shared `Events` feature. Shared event exceptions remain directly under Application `Events`.

Infrastructure remains organised as `Persistence` (context, mappings inside the context, repository), `Notifications` (delivery adapter), and root `DependencyInjection.cs` (registration). No mapping abstraction or project was introduced. Private `ValidatedEventDetails` and private test doubles remain inside their owners.

## Startup and compatibility

Run from the repository root:

```powershell
dotnet restore AppointmentScheduler.slnx
dotnet build AppointmentScheduler.slnx --no-restore
dotnet test AppointmentScheduler.slnx --no-build --no-restore
dotnet run --project src/AppointmentScheduler.Api/AppointmentScheduler.Api.csproj --no-build --launch-profile http
```

`dotnet run` uses the API project directory as its working directory. The configured connection string is now `Data Source=../../appointment-scheduler.db`, preserving the existing repository-root database. Use `ConnectionStrings__SchedulerDatabase` with an absolute path when launching from another directory or deploying. Launch profiles, ports, environments and package versions are retained. The old ignored `.csproj.user` and unrelated IDE state were preserved; reload the solution if the IDE still references the former host.

Baseline and final builds passed with zero warnings/errors; all 56 existing tests passed with zero failures/skips. Controller discovery, `WebApplicationFactory<Program>` content-root discovery, DI and validation worked with the renamed host assembly. No hard-coded assembly-name or reflection dependency required a source change.

The HTTP launch profile started Kestrel with a fresh disposable SQLite file. Creation returned 201, filtered search/list and update returned 200, attendance/cancellation returned 204, stale update returned 409, invalid inputs returned 400 and missing cancellation returned 404. Persisted attendance, cancellation and version were checked directly. Notifications logged Created once, Updated twice (update and attendance), and Cancelled once.

Baseline/refactor OpenAPI paths, schemas, validation and response metadata matched; the default document title reflects the new API assembly name. Smoke-test server ports differed. SQLite table/index/constraint definitions matched exactly. All 56 hand-authored type declarations/bodies and Program startup logic matched after excluding imports/namespaces; file/type/folder namespace correspondence was also checked. The original database SHA-256 remained unchanged.

## Diagram regeneration

`CodeMap1.dgml` is a stale generated snapshot of the former host assembly and source organisation. It was left untouched. Documentation Mermaid diagrams describe dependencies and runtime flow; they are explanatory views, not executable architecture or evidence that the refactor is correct. Path-specific documentation has been updated; regenerate any IDE diagrams or exported images derived from the previous layout.

1. Reload `AppointmentScheduler.slnx` and rebuild with the commands above.
2. In Visual Studio with Code Map support, generate a **new** code map from the current solution or selected source types. Include `AppointmentScheduler.Api`, Application, Domain and Infrastructure; include tests only when tracing test hosting.
3. Expand types from their matching files. Trace create from `EventsController` → `CreateEventHandler` → `CalendarEvent`/`Attendee` → `IEventRepository`/`EfEventRepository` → `SchedulerDbContext`, then `IEventNotificationPublisher`/`LoggingEventNotificationPublisher`. Interface implementation links and DI composition determine concrete runtime targets.
4. Save the regenerated map separately and verify namespace, assembly and source links before replacing the historical snapshot. Re-render documentation Mermaid blocks using the documentation viewer when an exported diagram is needed.

## Remaining issues

No new correctness issue was identified or repaired during this refactor. Existing limitations remain: `EnsureCreated` provides no schema migration, list results have no pagination, notification delivery is simulated and a publisher failure occurs after persistence, and generated-client tooling is deferred. No generated source, existing database, dependencies, unrelated IDE files, commits or remote state were reorganised or published.
