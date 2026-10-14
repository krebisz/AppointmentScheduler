# Artifact grouping and diagram navigation

Verified 9 October 2026. The user prefers directories that expose the kinds of artifacts in each project. The four architectural projects remain; folders now group by artifact rather than operation.

## Current structure

```text
src/
  AppointmentScheduler.Api/
    Controllers/ Requests/ Responses/ ErrorHandling/ OpenApi/ Properties/
    Program.cs and host configuration
  AppointmentScheduler.Application/
    Handlers/ Commands/ Queries/ Inputs/ Results/ Interfaces/ Exceptions/
    Notifications/ (message and associated enum)
  AppointmentScheduler.Domain/
    Entities/ Inputs/ Exceptions/ Validation/
  AppointmentScheduler.Infrastructure/
    DbContexts/ Configurations/ Repositories/ DatabaseInitialization/
    Migrations/ NotificationPublishers/
    InfrastructureServiceCollectionExtensions.cs
tests/
  AppointmentScheduler.Tests/
    Api/Controllers/ Fixtures/ Validation/ ErrorHandling/ OpenApi/
    Application/Handlers/
    Domain/Entities/
    Infrastructure/Repositories/ DatabaseInitialization/ NotificationPublishers/
```

Each listed test concern is beneath its corresponding boundary; for example Fixtures is under Api. One hand-authored top-level type has its matching file/folder namespace. Program, private nested helpers and generated migration conventions retain their appropriate arrangements.

## Reasoning and navigation

Artifact grouping supports scanning structural composition: all controllers, requests, responses or handlers are visible together. This is a navigation preference, not a new architectural pattern. Operation names connect CreateEventController, CreateEventRequest, CreateEventCommand, CreateEventHandler and CreateEventResult across these directories. Separate boundary messages and narrow repository/publisher interfaces still isolate HTTP, business rules and provider concerns.

Domain Inputs are immutable operation payloads, not additional persistent entities or automatically value objects. Notification payload and its enum stay together because they form one message contract; a generic Enums bucket would separate tightly related declarations. Infrastructure contexts, mappings, repositories, initialization and migrations remain distinguishable artifacts without an outer Persistence container.

| Before this pass | Current grouping |
| --- | --- |
| API Create/Get/List/Update/Cancel/Attendance | Controllers, Requests, Responses |
| Application operation folders | Handlers, Commands, Queries, Inputs, Results |
| Application persistence/notification interfaces | Interfaces |
| Application exceptions at root/in List | Exceptions |
| Domain aggregate/child and input records at root | Entities, Inputs, Exceptions |
| Infrastructure Persistence with nested Migrations | DbContexts, Configurations, Repositories, DatabaseInitialization, Migrations |
| Infrastructure Notifications adapter | NotificationPublishers |
| API test hosting/session helpers at root | Fixtures |
| Operation-based tests | Grouped by tested artifact/concern |

76 files moved in this pass. The earlier redundant Events-container removal is retained. Existing uncommitted code/document work is extended rather than reverted; no staging, commit or publication occurred.

## Compatibility and verification

C# source paths/namespaces changed; source consumers must update imports. Assemblies/type names, HTTP bodies/routes/statuses/metadata, migration ID, database tables and save/notification/concurrency rules are unchanged. Generated EF snapshot/designer CLR names/imports were synchronized; no new migration is needed.

Baseline and final build: 0 warnings/errors; 123 tests pass. The first relocation build identified a missing AddInfrastructure extension import; it was restored before the successful final build/test. EF reports no pending model changes; generated SQL and full OpenAPI documents are exactly equal before/after. A populated disposable migration-managed database restarted with identical event/attendee IDs, attendance, version, schema/history. Fresh initialization also succeeds.

Real Kestrel smoke verified create/Location, retrieval, email swaps with retained IDs/attendance, timestamp rejection, stale conflict, attendance, cancellation/repetition, cancelled GET, listing, unknown-ID 404 and Swagger. Static checks confirm unchanged implementation bodies after import/namespace normalization, matching file/type/namespaces and unchanged configuration/original brief/working database.

Required timestamps, guarded migrations and stable identity remain implemented. Publication can still fail after saving; POST retry can duplicate creation; independent attendee replies still share one event version. Artifact organization does not change these guarantees. Legacy database adoption remains deliberately blocked.

## DGML guidance

CodeMap1.dgml remains a historical generated snapshot. Regenerate from the built AppointmentScheduler.slnx in a Visual Studio edition supporting Code Map/DGML, inspect source links and save separately before replacing it. Start at the four projects, then expand artifact groups or follow one named operation through its call chain. Keep compile-time references separate from runtime calls; exclude generated migration detail when useful.

No new DGML generation or Visual Studio rendering was performed during this pass.
