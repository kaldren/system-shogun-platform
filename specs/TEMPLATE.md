<!--
Template for specs/<N>_<short-description>.md.
Copy this file, fill in the frontmatter and the sections relevant to `scope`,
and delete any per-domain sections that don't apply.
-->
---
number: <N>
feature: <short-description>
status: draft # draft | approved | in-progress | implemented
owner: <approver>
created: <YYYY-MM-DD>
scope: [backend] # subset of: frontend, backend, infra
---

# Spec <N>: <Feature Title>

## Problem Statement

What problem is this solving? Why does it matter now?

## Goals / Non-Goals

- Goals: ...
- Non-Goals: ...

## Requirements

Functional and non-functional requirements, as a list.

## Constraints & Assumptions

Anything that limits the design (existing systems, deadlines, team size, etc.)
and anything being assumed without confirmation.

## Proposed Architecture

High-level approach. Break into subsections matching `scope`:

### Backend

(Only if `backend` is in scope.)

### Frontend

(Only if `frontend` is in scope.)

### Infra

(Only if `infra` is in scope.)

## API Contract

(Only if `backend` is in scope.) Endpoints, request/response shapes, error cases.

## Data Model

(Only if `backend` is in scope.) Entities, relationships, storage choices.

## UI/UX Notes

(Only if `frontend` is in scope.) Screens/components involved, states, interactions.
Must be consistent with `design-system.md` — call out anything that isn't.

## Infra/Deployment Notes

(Only if `infra` is in scope.) Azure resources, Bicep modules, environments, CI/CD impact.

## Trade-offs & Alternatives Considered

What else was considered, and why this approach was chosen.

## Risks

What could go wrong, and mitigations.

## Open Questions

Anything unresolved that the approver should weigh in on before approving.

## Testing Plan

How this will be verified (unit/integration tests, manual verification steps).
