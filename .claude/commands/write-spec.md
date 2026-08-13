---
description: Draft a new feature spec via the spec-writer agent. Stops after writing the draft — never proceeds to implementation.
argument-hint: <feature description>
---

The user wants a new feature spec for: $ARGUMENTS

Do the following, and nothing more:

1. Dispatch the `spec-writer` subagent with the feature description above. Give it the full
   description verbatim plus an instruction to follow its own process (assign the next spec
   number, fill in `specs/TEMPLATE.md`, ask clarifying questions if needed, write
   `specs/<N>_<short-description>.md` with `status: draft`).
2. Once the spec-writer agent finishes, report back to the human: the spec's file path,
   number, and a one-paragraph summary of what it covers.
3. Tell the human explicitly: this spec is a **draft** and nothing will be implemented until
   they review it and run `/approve-spec <N>`.

**Do not** invoke the `backend`, `frontend`, or `infra` agents in this command, no matter how
small or obviously-correct the feature looks. Approval is a hard gate, not a formality — this
command's only job is producing a draft spec and stopping.
