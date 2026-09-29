# Worklog — Doctorly technical task

This is the reviewer-facing record of prioritisation, decisions, delivery sequence, verified evidence, and deferred scope. Replace stale entries rather than accumulating an agent transcript. `ASSESSMENT.md` and the PDF define requirements.

## Snapshot

- Remaining budget: not re-estimated after Phase 5; preserve the final 30 minutes for handover
- Environment/setup time lost: approximately 45 minutes to sandbox failures, Google Drive file locking/path issues, relocation, and repository realignment
- Last verified: 2026-09-29 13:53 +02:00
- Handover reserve: final 30 minutes
- Current slice: centralized API error handling and null-input regression coverage - verified by full solution tests
- Next action: final runtime smoke, repository audit, and handover

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
| Must | Appropriate tests; runnable solution | Verified | 56 tests pass; solution compilation succeeds. |
| Explicit design | EF or similar, storage, layering, interfaces, DDD patterns | Verified | Separate Domain, Application, Infrastructure and API projects/boundaries; EF Core SQLite repository implements the application port. |
| Should | OpenAPI | Verified | Development document returned HTTP 200, OpenAPI 3.1.1, with /api/events. |
| Should | Generated client | Deferred | OpenAPI JSON is available as generation input; generator/tooling and distributed client source were not added because they were disproportionate to the assessment slice. |
| Should | Public-facing generated docs | Verified, limited | Swagger UI renders the generated OpenAPI document in Development; production hosting is deferred. |
| Should | Accept/reject event | Verified, limited | PATCH updates one attendee's `isAttending` value; invitation workflow/history is deferred. |
| Could | Same-event simultaneous updates | Verified, limited | Numeric optimistic version rejects stale requests with 409 and EF rejects racing saves; retries, merge, ETags, and audit history are deferred. |
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
| Swagger UI over the existing OpenAPI document | Adds reviewer-facing interactive documentation with one small package/configuration change. It is Development-only for this assessment. |
| Defer generated client tooling | The OpenAPI document is a valid future generator input, but selecting, configuring, compiling, and distributing a client adds disproportionate concepts for the remaining scope. No generated client is claimed. |
| Numeric optimistic concurrency on the Event aggregate | Reuses one `Version` value across update and attendance writes. The application rejects stale client versions and EF Core detects a race at save time. This is deliberately simpler than ETags, merge/retry, or distributed locks. |
| Attendance response is the existing attendee boolean | PATCH changes only `isAttending` and returns 204. It demonstrates accept/reject without inventing invitation workflow, response history, comments, or separate reservation entities. |
| Test at the boundary where the risk lives | Domain tests cover invariants; application fakes cover orchestration and ordering; SQLite tests cover relational/provider behavior; `WebApplicationFactory` tests cover the real HTTP pipeline and error contract. This balances speed with confidence without repeating every assertion at every layer. |
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

### 80/20 scope boundary

- User explicitly excluded backward compatibility: the temporary schema-upgrade implementation and test were removed. EnsureCreated supports fresh databases only; existing data was not changed or deleted.
- Expected exceptions map centrally in ApiExceptionHandler to 400/404/409; unexpected failures are logged with a trace ID and return a generic 500 ProblemDetails in all environments. Controllers retain request validation and HTTP mapping, with no repeated exception catches.
- Null attendee elements return 400; required fields/collections/bodies use API validation. Attendance PATCH requires an explicit boolean response so omitted/null values cannot silently reject.
- Persistence errors propagate and prevent notification publication. A publisher error after persistence propagates as 500 without rolling back saved data; no automatic retry or outbox is claimed.

- Swagger UI is human documentation, not a generated client; the client requirement remains explicitly deferred.
- Concurrency demonstrates detection and a clear 409 response, not conflict resolution.
- Accept/reject changes one attendee's boolean response by stable identifier; it is not a broader invitation or reservation workflow.

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
| 7 | Lean Swagger UI, attendee response, and optimistic-concurrency demonstrations | Cover selected Should/Could items without introducing a client-generation toolchain or workflow platform. |

## Verification

| Command / request | Observed result |
| --- | --- |
| dotnet clean -> restore -> build AppointmentScheduler.slnx | Passed from clean output; all projects restored, 0 warnings, 0 errors. |
| dotnet test AppointmentScheduler.slnx --no-restore | Passed after compiling all projects; 56 passed, 0 failed, 0 skipped. |
| Error regression tests | Safe 500 responses across all five API operations; 400 for null/missing/malformed inputs; 404 for missing events/routes; save conflicts 409; failed saves do not publish; publisher failure leaves persisted data; Production errors contain no internal exception details. |
| Lean optional-feature tests | Swagger UI returned 200; attendee PATCH persisted its boolean response; stale API update returned 409; two EF writers caused the second save to raise the mapped concurrency exception. |
| dotnet run --project AppointmentScheduler.csproj --no-build --launch-profile http | Started cleanly in Development on http://localhost:5158 using a fresh disposable SQLite database; GET /api/events returned HTTP 200 with an empty array. |
| Fresh-database HTTP lifecycle smoke on port 5170 | Invalid create 400; create 201; combined overlap filter/search 200 with one match; update 200; inverted range 400; cancel 204 then 204; default list empty; includeCancelled returned the cancelled event; cancelled update 400; unknown cancel 404. |
| Runtime notification logs | Exactly one simulated Created, Updated, and Cancelled log appeared after the corresponding EF writes; the repeated cancellation emitted no duplicate. |
| GET /openapi/v1.json | HTTP 200; OpenAPI 3.1.1 document contains /api/events. |
| Fresh-database lean feature smoke on port 5160 | Swagger UI 200; create version 1; attendance PATCH 204 and persisted true at version 2; first update 200; repeated stale update 409. |
| Reviewer documentation audit | README is the entry point and links the original brief, requirements transcription, and worklog. Usage, architecture/dependency direction, repository layout, create-event vertical slice, direct source links, test methodology, phase commits, scope, and limitations were checked against the project files and verified behavior. |
| Repository audit | No tracked build/database artifacts or obvious secret patterns found; the existing ignored development database was left untouched and the disposable smoke database was removed. |

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
| 4 | Minimal observable notifications with tests | Complete | d8636d9 - Add a minimal observable notification boundary and infrastructure implementation |
| 5 | Must-set stabilization and full smoke verification | Ready to commit | |
| 6 | Lean Swagger UI, attendee response, and optimistic concurrency | Complete | 2e620c0 - Add lean concurrency, attendance responses, and Swagger documentation |
| 7 | Final documentation, Git audit, commit/push, and submission preparation | Pending | |

## Handover

- Implemented and verified: create, list/filter/search, update, soft cancellation, simple attendee response, optimistic concurrency, aggregate invariants, relational SQLite persistence, observable simulated notifications, OpenAPI/Swagger documentation, focused domain/application/infrastructure/API tests
- Missing or unverified Must items: none currently identified
- Deferred Should/Could items: generated client, production documentation hosting, richer invitation workflow, automatic conflict resolution, availability checks
- Known limitations: startup uses EnsureCreated rather than migrations; existing development databases must be recreated after schema changes; list results are not paginated; validation errors use one event-level domain error key
- AI assistance: Phase 1-5 code/tests/documentation drafted with AI; clean build, tests, SQLite persistence, complete HTTP lifecycle behavior, simulated notification logging, OpenAPI output, reviewer startup, and repository hygiene executed locally
- Git status, final commit, remote/link: latest local commit verified as 2e620c0; centralized error handling, null validation, regression tests, and updated documentation remain uncommitted. Remote state not rechecked this turn.
- Commit-note clarification: the Phase 4 commit subject mentions failure handling, but the streamlined implementation intentionally defers external delivery-failure policy as documented above
