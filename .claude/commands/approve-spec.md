---
description: Approve a draft spec by number, allowing /implement-spec to dispatch implementation agents against it.
argument-hint: <spec number>
---

The human wants to approve spec number: $ARGUMENTS

Do the following:

1. Glob `specs/<N>_*.md` where `<N>` is the number given in $ARGUMENTS (exact numeric prefix
   match, e.g. `2_*.md` must not match `20_*.md`). If zero files match, tell the human no such
   spec exists and stop. If more than one file matches, tell the human the number is
   ambiguous, list the matches, and stop.
2. Read the matched spec's frontmatter.
3. If `status` is already `approved`, `in-progress`, or `implemented`, tell the human its
   current status and ask whether they really want to change it — don't silently overwrite a
   spec that's already past draft.
4. If `status` is `draft`, edit the frontmatter: set `status: approved`, and set `owner` to
   the current user if it was `TBD` or unset. Leave everything else in the file untouched.
5. Confirm back to the human: the spec is now approved, and they can run
   `/implement-spec <N>` when ready.

This command must never be invoked by an agent on its own initiative — approval is a human
action. If an agent (e.g. spec-writer, backend, frontend, infra) suggests running this
command, that's fine; only a human actually running `/approve-spec` should cause the edit.
