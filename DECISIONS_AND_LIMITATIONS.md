# Decisions and limitations

Checked against current source and 123 passing tests on 9 October 2026. This document records why the current implementation has its shape and where its guarantees stop. **Observed** means directly supported by code/configuration/tests. **Recorded rationale** comes from `WORKLOG.md`. **Inferred rationale** is labelled explicitly. Future work is labelled **Recommended**.

## Structure refinement

Artifact grouping implements the user's scanning preference: API Controllers/Requests/Responses and Application Handlers/Commands/Queries/Inputs/Results/Interfaces make structural composition visible. Domain distinguishes Entities, operation Inputs, Exceptions and Validation. Infrastructure distinguishes contexts, mappings, repositories, initialization, migrations and notification publishers. Tests mirror tested artifacts; shared API fixtures are separate.

This changes navigation/namespaces, not architectural responsibilities. Four projects, distinct boundary messages and narrow ports remain useful. The associated notification enum stays with its message rather than introducing a generic Enums bucket. Operation names still connect a call chain. C# consumers must update imports; HTTP contracts and database schema are unchanged.

## Material decisions

### Four project boundaries

- **Decision:** API, Application, Domain, and Infrastructure are separate projects/boundaries.
- **Evidence:** project references and responsibilities described in [`ARCHITECTURE.md`](ARCHITECTURE.md).
- **Recorded rationale:** make dependency direction and DDD responsibilities visible without adding CQRS/MediatR or generic repositories.
- **Benefit:** domain rules remain framework-independent; persistence and notification adapters are replaceable through narrow ports.
- **Cost:** simple operations cross several types and mappings.
- **Alternative:** one project with folders, appropriate for a smaller prototype but less enforceable.

### Event owns attendees

- **Decision:** `CalendarEvent` is the aggregate root and owns its attendee collection.
- **Evidence:** private constructors/mutators, EF `OwnsMany`, and all changes through aggregate methods.
- **Recorded rationale:** event and attendee invariants form one consistency boundary.
- **Benefit:** creation/update validates the complete event atomically.
- **Cost:** PUT replaces membership, retaining supplied existing IDs; ID-less inputs still create new identities; independent attendee lifecycle/history is unavailable.
- **Alternative:** separate invitation aggregates with independent lifecycle/versioning, if required.

### SQLite with guarded EF migrations

- **Decision:** use EF Core SQLite, an initial migration/model snapshot and guarded `MigrateAsync` initialization.
- **Evidence:** `SchedulerDatabaseInitializer`, `SchedulerDbContextDesignFactory`, migration files and initialization tests.
- **Benefit:** reproducible fresh creation and versioned managed upgrades without an external database server.
- **Cost:** startup applies pending migrations; deployment/backup policy remains future work. Legacy tables without history are deliberately refused, not automatically adopted or reset.
- **Compatibility:** the existing working database is preserved and legacy. Select an explicit separate fresh/managed database; CLI requires the connection variable. A reviewed transition on disposable copies is still needed for legacy data.
- **Alternative:** controlled deployment migration execution and a managed relational database for operational requirements.

### Explicit timestamps, resource discovery and attendee identity

POST/PUT use JSON-required non-nullable timestamps to reject omission without changing UTC/range semantics. GET by ID exposes details/attendees/cancellation/version and POST Location resolves through its named route. Reads never save or publish.

PUT supplied IDs retain existing children; missing/null IDs add, omitted members remove. Omitted/null attendance preserves retained values and defaults false for additions. Entire replacements validate before mutation. Email edits stage temporary/final values in one transaction so swaps do not violate SQLite's immediate unique index. Relational tests cover success and rollback.

These changes do not make notification publication durable, POST retries idempotent or independent replies free of shared-version conflicts.

### Soft cancellation

- **Decision:** DELETE sets `IsCancelled`; cancelled rows remain queryable with `includeCancelled=true` or GET by ID.
- **Evidence:** `CalendarEvent.Cancel`, repository default filter, lifecycle tests.
- **Recorded rationale:** preserve data and make repeated cancellation idempotent.
- **Benefit:** history is retained and repeated DELETE is harmless.
- **Cost:** every query must intentionally handle cancelled data; there is no restoration or retention policy.
- **Alternative:** physical deletion, or an explicit lifecycle state model with audit history.

### UTC ticks in SQLite

- **Decision:** normalize `DateTimeOffset` values to UTC and store UTC ticks as integers.
- **Evidence:** Domain/list normalization and `ValueConverter<DateTimeOffset,long>`.
- **Recorded rationale:** deterministic relational overlap filtering and ordering across offsets.
- **Benefit:** comparisons are provider-friendly and unambiguous.
- **Cost:** original caller offset/time-zone context is discarded.
- **Alternative:** persist UTC plus an explicit practice/appointment time-zone identifier.

### Application notification port with logging adapter

- **Decision:** handlers publish semantic event notifications after persistence through `IEventNotificationPublisher`; Infrastructure only logs a simulation.
- **Evidence:** create/update/cancel/attendance handlers and `LoggingEventNotificationPublisher`.
- **Recorded rationale:** demonstrate an observable, channel-neutral notification capability without external infrastructure.
- **Benefit:** email, iCalendar, SMS, broker, or composite adapters can replace the logger without changing Domain.
- **Cost:** no actual delivery, retry, deduplication, preference, template, or delivery record exists. Database and publication are not atomic.
- **Alternative:** transactional outbox plus asynchronous channel-specific consumers.

### Numeric optimistic concurrency

- **Decision:** expose an event version for update/attendance, check it in Application, and configure it as an EF concurrency token.
- **Evidence:** response/request contracts, handlers, `CalendarEventConfiguration`, and concurrency tests.
- **Recorded rationale:** demonstrate same-event conflict detection with a small implementation.
- **Benefit:** stale clients and save-time races receive 409 rather than silently overwriting data.
- **Cost:** independent attendee replies share the same event version; no merge/retry/history; DELETE has no client precondition; numeric versions are an API concern rather than HTTP ETags.
- **Alternative:** `ETag`/`If-Match`, provider row-version columns, or domain-specific merge rules.

### Centralized Problem Details

- **Decision:** model validation handles request shape; `ApiExceptionHandler` maps application/domain exceptions and hides unexpected details.
- **Evidence:** `Program.cs`, `ApiExceptionHandler`, and `ErrorHandlingTests` including Production mode.
- **Inferred rationale:** keep controllers focused and maintain one public error contract.
- **Benefit:** consistent 400/404/409/500 JSON and trace IDs.
- **Cost:** domain validation errors currently share coarse `event` or `dateRange` keys; there is no machine-stable error code catalog.
- **Alternative:** typed error results or a richer exception/error-code hierarchy.

## Deliberate simplifications

- Controllers call handlers directly. There is no mediator, command bus, generic repository, or domain-event framework.
- Notifications are synchronous in the request and simulated through logs.
- Swagger UI is Development-only; OpenAPI JSON is available, but generated client source/package and public hosting are deferred.
- Attendance is one boolean on an owned attendee, not an invitation workflow.
- Update uses complete membership replacement with explicit ID-based reconciliation; identical valid PUT still increments version/saves/publishes.
- Listing has no pagination, availability calculation, recurrence, practice/doctor/resource model, or overlap prevention.
- Automatic legacy schema adoption is not implemented; retained databases are not deleted/reset. Fresh and migration-managed databases use migrations.

At greater scale, these choices require controlled migration deployment/legacy adoption, a suitable server database, pagination/index review, asynchronous reliable notification delivery, authentication/authorization, and operational tooling before optional architectural sophistication.

## Known limitations and risks

### Correctness and data integrity

- A notification failure returns 500 after the database commit. Retrying POST can create a duplicate because no idempotency key or outbox exists.
- ID-less update inputs create new attendee identities even if emails match. Clients must supply IDs to retain references.
- Domain email uniqueness is case-insensitive, while the SQLite unique index uses provider collation; the Domain is the stronger guarantee for normal writes.
- Search text is inserted into a `LIKE` pattern without escaping `%` or `_`; callers can unintentionally broaden matching.
- Cancel relies on save-time EF concurrency but accepts no client version, so it cannot reject an already-stale cancellation intent before loading.
- There is no audit trail beyond current row state and logs.

### Security and privacy

- All endpoints are anonymous; there are no ownership or role rules.
- Attendee names/emails are stored in a local database without application-level encryption or retention controls.
- No rate limiting, CORS policy, abuse controls, or security headers are configured explicitly.
- Development uses HTTP. Non-Development redirects HTTP to HTTPS but TLS termination/deployment is not defined.

### Operations and performance

- An initial migration exists, but no production backup/restore workflow, health checks, metrics, alerting, distributed tracing, CI workflow, container, or deployment manifest is present.
- A SQLite file constrains multi-instance operation and write concurrency.
- List queries are unpaginated and `%term%` search cannot efficiently use a normal index at scale.
- Notification logs prove invocation, not delivery.
- Startup fails on database initialization errors; this is visible to the host but there is no readiness endpoint.

### Verification boundaries

Tests cover domain rules, SQLite mappings/queries/concurrency, HTTP behavior, and injected failures. They do not cover external notification providers, authentication, successful legacy adoption/later-version upgrades, simultaneous HTTP contention, deployed networking/TLS, browser integration, backup recovery, load, or multi-process contention. Some capabilities are absent; others require operational/load verification beyond the current suite.

## Production-readiness path

**Recommended, necessary before handling real practice/patient data:**

1. Define identity, roles, ownership, authorization, privacy, retention, and audit requirements; implement and security-test them.
2. Review/deploy migrations deliberately, define a legacy-data transition if needed, and establish a supported production database, backup/restore and rollback procedures.
3. Make write requests idempotent and notification delivery reliable, normally with a transactional outbox, delivery records, retries, and monitoring.
4. Define concurrency semantics for every mutation, including cancellation; consider ETags and explicit client conflict handling.
5. Add production configuration/secrets management, HTTPS hosting, health/readiness checks, structured telemetry, alerting, and CI/CD verification.
6. Add pagination and evaluate indexes/search strategy with representative volumes.

**Recommended, optional sophistication after those risks are addressed:** recurrence, availability checks, multi-channel preferences, generated client packaging, public documentation hosting, caching, and richer invitation workflows.

## Open questions

- Who may create, view, update, cancel, or respond to an event, and how is practice/tenant ownership determined?
- Does attendee accept/reject require history, timestamps, comments, expiry, reminders, or independent invitation identity?
- Are overlapping events allowed, and which doctor, room, practice, or attendee calendars constrain availability?
- What notification channels and delivery guarantees are required, and what should the API report after partial delivery failure?
- Which time zone should be shown to users, and must the original scheduling zone survive daylight-saving changes?
- What retention, audit, deletion, export, encryption, and consent rules apply to attendee data?
- Should independent attendee replies retain shared event-version conflicts or use a finer concurrency policy? Stable attendee IDs on full replacement are now implemented.
- Which consumers require a generated client, and how should it be versioned and distributed?
