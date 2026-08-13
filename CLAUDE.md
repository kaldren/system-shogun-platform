# SystemShogun — Project Rules

## Stack

- **Frontend**: Blazor WebAssembly (standalone), calling the backend over HTTP. Lives in `src/Frontend/`.
- **Backend**: ASP.NET Core Web API (C#/.NET). Lives in `src/Backend/`.
- **Infra**: Azure, infra-as-code via Bicep. Lives in `infra/`.

## Repo layout

- `src/Frontend/` — Blazor WASM client.
- `src/Backend/` — ASP.NET Core Web API.
- `infra/` — Bicep templates and deployment config.
- `specs/` — feature specs (see below). `specs/TEMPLATE.md` is the required shape.
- `design-system.md` — the binding source of truth for frontend look-and-feel (fonts, colors, spacing, component conventions). The frontend agent must follow it for every UI change.
- `docs/` — general documentation, ADRs, etc.

## Spec-driven development — hard rule

**No implementation work happens on a feature without an approved spec in `specs/`.** This applies to the human and to every agent. If you (agent or human) are asked to "just implement X" and no approved spec for X exists in `specs/`, the correct response is to redirect to `/write-spec` first — not to start coding.

The workflow has three steps, each a slash command:

1. **`/write-spec <description>`** — dispatches the `spec-writer` subagent, which drafts a new spec at `specs/<N>_<short-description>.md` (next available number `N`) with `status: draft`, following `specs/TEMPLATE.md`. Then it stops. No implementation agent runs at this stage, no matter how small the feature looks.
2. **`/approve-spec <N>`** — human-run. Flips `status: draft → approved` on `specs/<N>_*.md`. This is the *only* sanctioned way a spec becomes approved — no agent may set `status: approved` itself.
3. **`/implement-spec <N>`** — checks `specs/<N>_*.md` has `status: approved`; refuses otherwise. If approved, dispatches the `backend`, `frontend`, and/or `infra` subagents per the spec's `scope:` field.

Specs are numbered (`1_...`, `2_...`, …) and addressed by number in `/approve-spec` and `/implement-spec`.

## Agents

Four project subagents live in `.claude/agents/`:

- **spec-writer** — writes specs, never implementation code.
- **backend** — implements ASP.NET Core Web API work, only against an approved spec.
- **frontend** — implements Blazor WASM work, only against an approved spec, and must follow `design-system.md`.
- **infra** — implements Bicep/Azure work, only against an approved spec.
