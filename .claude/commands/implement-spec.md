---
description: Implement an approved spec by dispatching the backend/frontend/infra agents it declares in scope.
argument-hint: <spec number>
---

The human wants to implement spec number: $ARGUMENTS

Do the following:

1. Glob `specs/<N>_*.md` where `<N>` is the number given in $ARGUMENTS (exact numeric prefix
   match, e.g. `2_*.md` must not match `20_*.md`). If zero files match, tell the human no such
   spec exists and stop. If more than one file matches, tell the human the number is
   ambiguous, list the matches, and stop.
2. Read the matched spec's frontmatter.
3. **Gate check**: if `status` is not `approved`, refuse to dispatch any implementation
   agent. Report the current status back to the human and tell them to run
   `/approve-spec <N>` first. Stop here — do not proceed to step 4 under any circumstances,
   even if the spec looks simple or the human seems impatient.
4. Read the spec's `scope` field. It will contain some subset of `backend`, `frontend`,
   `infra`.
5. Dispatch agents based on scope, in this order:
   - If `backend` and/or `infra` are in scope, dispatch those agents first — in parallel with
     each other if both are present, since neither depends on the other. Give each agent the
     spec's file path.
   - If `frontend` is in scope, dispatch the `frontend` agent only after backend/infra (if
     any were in scope) have finished, since the frontend consumes the API contract the
     backend produces. If `frontend` is the only item in scope, dispatch it directly.
6. Once all dispatched agents finish, update the spec's frontmatter `status` to
   `implemented`, and report a summary back to the human: what was implemented, by which
   agents, and any open items or assumptions those agents flagged (e.g. design-system
   assumptions from the frontend agent).

If any dispatched agent reports back that it refused to act (e.g. its own gate check failed,
or it hit something unexpected), stop and surface that to the human rather than continuing to
the next agent or marking the spec implemented.
