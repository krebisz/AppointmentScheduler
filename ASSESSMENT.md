# Doctorly Senior .NET technical task

Source: `NET Technical Test.pdf` supplied 29 September 2026. This is a structured transcription for implementation; the original PDF is authoritative. The accompanying email clarifications are listed below. Do not add implementation status to this file.

## Purpose and delivery

- Maximum four hours; complete as much as possible. A complete solution is **not expected**, but the code must compile and run.
- Work in Git as you would professionally. Provide thoughts, choices, assumptions, useful commit messages, appropriate tests/documentation, a sensible file structure, and instructions to run/use the solution.
- Submit a repository link by replying to all email participants (Loic and Vincent included). The PDF also mentions ZIP, but the accompanying email specifically asks for a repo link.
- The assessment considers approach, prioritisation, communication, technical ability, and implementation. Ambiguous details may be resolved through documented reasonable assumptions.

## Technology and design

- Use Git and any IDE.
- The PDF says .NET 5; **the accompanying email explicitly says to disregard the .NET 5 requirement**. Choose and document an available supported version.
- Use Entity Framework or similar. Data should be stored somewhere; the PDF explicitly allows in-memory storage or a real database.
- Layer the solution appropriately using suitable patterns, abstractions, and interfaces; use DDD patterns.
- Build the backend API and services for a doctor's-practice calendar/schedule; no frontend is needed.

## Requirements: Must

- Attendee: at least Name, Email Address, and whether they are Attending.
- Event: at least Title, Description, Attendees, Start Time, and End Time.
- Sensible field-size limits.
- Notification capability (Email/iCal/MQ/other); mechanism unspecified.
- Appropriate tests.

### API functions: Must

- Create event.
- Update event.
- Delete/cancel event.
- List calendar events with filters.
- Search for events.

## Requirements: Should

- Implement the OpenAPI specification.
- Provide an auto-generated client consumable by third parties.
- Provide public-facing auto-generated API documentation.
- Accept/reject event API operation.

## Requirements: Could

- Address simultaneous updates to the same event and preservation of data.
- Add other advanced features if useful, such as attendee availability checks.

## Scope note from the PDF

There is insufficient time for everything. Scaffolding can demonstrate intention, but the submission should show architectural choices and a functioning vertical slice or a justified deeper implementation, together with clear instructions and appropriate tests. Record what actually works and what remains incomplete.

## Accompanying email

- Doctorly may not be available for questions; make and document reasonable assumptions.
- Use the four hours to complete as many instructions as possible. They are looking at approach, communication, and choices, rather than a complete solution.
- Reply to all participants with the Git repository link.
- “Please don't mind the .NET 5 requirement.”
