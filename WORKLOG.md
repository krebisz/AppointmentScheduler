# Worklog — Doctorly technical task

Keep this a short live dashboard. Replace stale entries; report only verified outcomes. `ASSESSMENT.md` and the PDF define requirements.

## Snapshot

- Remaining budget: approximately 2 hours 30 minutes
- Implementation budget: approximately 2 hours, preserving the final 30 minutes for handover
- Environment/setup time lost: approximately 45 minutes to sandbox failures, Google Drive file locking/path issues, relocation, and repository realignment
- Last verified: 2026-09-29 12:33 +02:00
- Handover reserve: final 30 minutes
- Current slice: minimal observable notifications - verified and ready to commit
- Next action: Must-set stabilization and final handover verification

## Coverage

`Not started` / `In progress` / `Verified` / `Deferred` / `Blocked` refer to this repository, never the previous practice project.

| Priority | Requirement | Status | Evidence or gap |
| --- | --- | --- | --- |
| Must | Event and attendee fields; size limits | Verified | Domain and API enforce title 200, description 2,000, attendee name 200, email 320; Event owns Attendees. |
| Must | Create event | Verified | POST /api/events returned HTTP 201 and persisted Event/Attendee rows to SQLite. |
| Must | Update event | Verified | PUT /api/events/{id} replaces event details and attendees; missing returns 404 and cancelled events reject updates. |
| Must | Delete/cancel event | Verified | DELETE /api/events/{id} soft-cancels idempotently; cancelled events are excluded from lists unless includeCancelled=true. |
| Must | List with filters | Verified | GET /api/events supports inclusive overlap filters using from/to and returns UTC start-time order. |
| Must | Search events | Verified | The same endpoint supports case-insensitive title/description search, combinable with date filters. |
| Must | Notification capability | Verified | Create, update, and first cancellation publish after persistence through an application port; infrastructure emits a simulated structured log without recipient addresses or external delivery. |
| Must | Appropriate tests; runnable solution | Verified | 18 focused tests pass; solution builds with 0 warnings/errors. |
| Explicit design | EF or similar, storage, layering, interfaces, DDD patterns | Verified | Separate Domain, Application, Infrastructure and API projects/boundaries; EF Core SQLite repository implements the application port. |
| Should | OpenAPI | Verified | Development document returned HTTP 200, OpenAPI 3.1.1, with /api/events. |
| Should | Generated client | Not started | |
| Should | Public-facing generated docs | Not started | |
| Should | Accept/reject event | Not started | |
| Could | Same-event simultaneous updates | Not started | |
| Could | Attendee availability or other advanced features | Not started | |

## Assumptions and material choices

| Choice / assumption | Reason and cost |
| --- | --- |
| .NET SDK 10.0.401 and net10.0 | This is the only installed SDK; the email explicitly waives .NET 5. |
| ASP.NET Core Web API with controllers and OpenAPI enabled | Preserves the supplied controller template and makes HTTP contracts explicit. No authentication, frontend, or containers are required. |
| EF Core SQLite 10.0.12 with startup EnsureCreated | Provides verified relational persistence with no external server. Migrations are deferred; a fresh local database is created automatically. |
| Event aggregate root owns Attendees | Enforces event time, field limits, at least one attendee, valid email, and case-insensitive unique attendee emails in one consistency boundary. |
| Accept offset-aware timestamps and persist UTC | Avoids local-time ambiguity; end time must be later than start time. |
| Store normalized UTC timestamp ticks in SQLite | Integer storage makes relational overlap filtering and ordering deterministic across offsets. |
| Filter by inclusive event overlap; search title/description | An event matches when it overlaps the requested from/to range. Search uses SQLite LIKE semantics and results are ordered by start time then ID. Pagination is deferred for the assessment scope. |
| Soft cancellation with default query exclusion | DELETE is idempotent and preserves data. Cancelled events remain inspectable through includeCancelled=true and cannot be updated. |
| Separate Domain, Application, Infrastructure, and API boundaries | Project references enforce Domain <- Application <- Infrastructure, while the API composes Application and Infrastructure. Only the persistence boundary has an interface. |
| Deliver vertical slices through those boundaries | The create-event slice now compiles, runs, persists and covers its invalid-time failure path. |
| Implement notification capability after the core event lifecycle, but before Should/Could features | Notifications depend on established event operations and are an infrastructure concern, but remain a Must requirement. |
| Provisional generated-client approach: expose OpenAPI and use NSwag to generate a C# client | Swagger/OpenAPI UI is human-facing documentation; it does not supply third parties with callable client code. NSwag can consume the OpenAPI document and generate typed C# methods/models. Validate this early enough to fall back or defer honestly if tooling cost is disproportionate. |
| Generated-client deliverable means source plus reproducible generation, not package publication | Supply generated C# client code, pinned tool/configuration or exact command, and a minimal usage example. NuGet publication is not requested. |
| Keep assessment artifacts; ignore unrelated/local/generated clutter | Phase checkpoints are committed on main. The precise ignore file covers build/IDE output, user files, SQLite databases, temporary PDF renders, and local agent guidance. |

### Phase 4 notification design

| Decision | Phase 4 choice |
| --- | --- |
| Trigger ownership | Application use cases request notifications only after successful persistence for event creation, update, and the first transition to cancelled. A repeated idempotent DELETE must not publish a duplicate cancellation notification. |
| Application boundary | Application defines a channel-neutral publisher interface and semantic event-notification message. Lifecycle use cases call the port directly after persistence; the Domain remains unaware of delivery mechanisms. |
| Message and recipients | Publish one event-level message containing the notification type, event identifier, relevant schedule details, and intended attendee email recipients. The adapter owns any later per-recipient or per-channel fan-out. |
| Initial implementation | Infrastructure supplies an in-process structured-logging adapter. It records notification type, event identifier, and recipient count, and is explicitly simulated: it must not claim that email or another external message was delivered. |
| Observability and tests | Focused tests use a capturing fake to verify the requested message, recipients, trigger, and persistence-before-publication ordering. The logging adapter provides runtime observability without adding a notification API. |
| Extension path | Email, iCalendar, SMS, or broker adapters can implement the same boundary; a composite publisher can add multi-channel dispatch later without changing the Domain or event use cases. |
| Explicitly deferred | External delivery, delivery-failure policy, channel preferences, templates, retries, deduplication, delivery records, and a transactional outbox are outside this assessment slice. These can be added behind the established boundary if production reliability is required. |

### Generated-client decision clarification

- **OpenAPI document:** machine-readable API contract describing operations, parameters, schemas, and responses.
- **Swagger UI or another API UI:** interactive human documentation rendered from that contract; useful for exploring and manually calling endpoints.
- **Generated client:** C# source compiled into a consumer application. It exposes typed methods and models and handles HTTP request construction and JSON serialization.
- Current proposed flow: API produces an OpenAPI JSON document -> NSwag reads it -> generated C# client is committed with a repeatable generation command/configuration -> a small example or test demonstrates consumption.
- Do not claim this Should requirement is complete until the client is generated, builds, and can call or be meaningfully smoke-tested against the API.
- If NSwag cannot be integrated proportionately within the remaining time, preserve the OpenAPI contract and document the client as deferred. Do not replace the client requirement with Swagger UI.

## Decisions still requiring explicit semantics

These are not settled by the PDF and must be decided before their slice is implemented:

| Decision | Options to resolve and document |
| --- | --- |
| Attendance | Default value and whether accept/reject updates one attendee by stable identifier. |

## Sequential implementation plan

This is the agreed working plan, not a change to the employer's priorities. Adjust to actual remaining time and explain material deviations. Layers define responsibility; slices deliver working use cases across layers.

| Step | Deliverable and boundaries | Why this slice |
| --- | --- | --- |
| 1 | Inspect/bootstrap Web API, select SDK and SQLite/EF setup; outline Event aggregate, attendee ownership, and invariants | Establish only the foundation needed for the first operation. |
| 2 | Create event: HTTP DTO/controller, application orchestration, domain validation, EF save, focused tests | Demonstrate a runnable end-to-end write path. |
| 3 | List, filter, and search: API query contracts, application query handling, EF queries, tests | Make stored data observable and deliver required reads. |
| 4 | Update and cancel: HTTP operations, domain transitions, persistence, tests | Complete the Must event lifecycle. |
| 5 | Minimal observable notifications: application trigger, infrastructure adapter, and focused tests | Complete the remaining Must capability without external-delivery risk. |
| 6 | Fresh build/run, tests, README, ignore/tracked-file audit, Git state, and submission preparation | Preserve the final 30 minutes for an honest runnable handover. |
| 7 | Generated client, public docs UI, accept/reject, or other Should items only if every Must item is stable | Do not trade required stability for optional breadth. |

Still decide and record only the attendance semantics before attempting that Should item. This is an assumption where the PDF is silent, not an extra employer requirement.

## Verification

| Command / request | Observed result |
| --- | --- |
| dotnet build AppointmentScheduler.slnx | Passed; 0 warnings, 0 errors. |
| dotnet restore AppointmentScheduler.slnx | Passed; all projects up-to-date for restore. |
| dotnet test AppointmentScheduler.slnx | Passed; 18 passed, 0 failed, 0 skipped after simplifying notification orchestration. |
| POST http://127.0.0.1:5158/api/events | HTTP 201; returned event and attendee IDs; EF logs confirmed inserts to Events and Attendees. |
| GET /api/events?from=2030-01-01T09:30:00Z&to=2030-01-01T10:30:00Z&search=cardiology | HTTP 200; returned only the overlapping matching event; EF logs confirmed server-side SQLite filtering and ordering. |
| POST -> PUT -> DELETE twice -> GET lifecycle smoke | HTTP 201, 200, 204, 204; default query returned no cancelled event and includeCancelled=true returned the updated cancelled event. |
| POST /api/events Phase 4 smoke on port 5169 | Streamlined implementation persisted and returned the event; runtime emitted the simulated Created notification log with event ID and recipient count after EF inserts. Disposable SQLite files were removed. |
| GET http://127.0.0.1:5158/openapi/v1.json | HTTP 200; OpenAPI 3.1.1 document contains /api/events. |

## Time and priorities

- Initial priority / smallest runnable slice: Event aggregate and attendees -> create use case -> SQLite repository -> POST endpoint -> focused tests
- Midpoint adjustment: environment/setup delay reduced the remaining implementation window to approximately 2 hours; prioritize the complete Must lifecycle, persistence/tests, then minimal observable notifications
- Handover cutoff: stop feature work with 30 minutes remaining for fresh verification, documentation, Git cleanup, and submission

## Phase checkpoints

Record each checkpoint only after its commit is verified in Git.

| Phase | Deliverable | Status | Commit |
| --- | --- | --- | --- |
| 1 | Create-event vertical slice, persistence, OpenAPI, and focused tests | Complete | 7a0c10b - Commit 1: Event aggregate, attendees, validation, application use case, EF Core/SQLite persistence, controller endpoint, OpenAPI, and focused tests. |
| 2 | List, filter, and search with SQLite-backed tests | Complete | 4c6bfa9 - Implement event listing filtering and search |
| 3 | Update and cancel with lifecycle tests | Complete | 6d08132 - Implement event update and cancellation |
| 4 | Minimal observable notifications with tests | Ready to commit | |
| 5 | Must-set stabilization and full smoke verification | Pending | |
| 6 | Should items, only if the Must set is stable | Conditional | |
| 7 | Final documentation, Git audit, commit/push, and submission preparation | Pending | |

## Handover

- Implemented and verified: create, list/filter/search, update, soft cancellation, aggregate invariants, relational SQLite persistence, observable simulated notifications, OpenAPI document, focused domain/application/infrastructure/API tests
- Missing or unverified Must items: none currently identified
- Deferred Should/Could items: generated client, public documentation UI, accept/reject, simultaneous-update handling, availability checks
- Known limitations: startup uses EnsureCreated rather than migrations; list results are not paginated; validation errors use one event-level domain error key
- AI assistance: Phase 1-4 code/tests/documentation drafted with AI; build, tests, SQLite persistence, HTTP lifecycle behavior, simulated notification logging, and OpenAPI output executed locally
- Git status, final commit, remote/link: Phase 3 committed as 6d08132 on main and tracks origin/main; Phase 4 changes are verified and ready for checkpoint commit
