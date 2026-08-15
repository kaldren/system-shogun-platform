---
number: 1
feature: aspire-orchestration
status: implemented
owner: Kaloyan Drenski
created: 2026-08-14
scope: [backend, frontend]
---

# Spec 1: .NET Aspire Orchestration (AppHost + ServiceDefaults)

## Problem Statement

Today `src/Backend` (ASP.NET Core Web API) and `src/Frontend` (standalone Blazor WebAssembly)
are two independent projects with no wiring between them. Each has its own `launchSettings.json`
with hardcoded local ports (Backend: `https://localhost:7024` / `http://localhost:5258`,
Frontend: `https://localhost:7026` / `http://localhost:5056`), and the Frontend's only
`HttpClient` registration points at its own host address (`builder.HostEnvironment.BaseAddress`),
not at the Backend. There is currently no code path in either project that calls the other, no
CORS configuration on the Backend, and no single command that starts both together — a developer
has to run `dotnet run` twice, in two terminals, and manually keep port numbers in sync.

As the app grows and the Frontend starts calling Backend APIs, this will get worse: more services
likely means more hardcoded URLs, more manual coordination, and no unified local logs/traces
across the two processes. .NET Aspire solves exactly this class of problem for local
orchestration: a single `AppHost` project starts and coordinates all services, injects endpoint
configuration between them (service discovery), and gives a unified dashboard for logs, traces,
and metrics. `ServiceDefaults` centralizes the cross-cutting OpenTelemetry/health-check/resilience
wiring so it isn't duplicated per service.

## Goals / Non-Goals

- Goals:
  - Add a `SystemShogun.AppHost` project that, via `dotnet run` (or F5) from a single entry
    point, starts both Backend and Frontend together.
  - Add a `SystemShogun.ServiceDefaults` project with shared OpenTelemetry, health-check, and
    HTTP resilience/service-discovery wiring, referenced by the Backend (see Open Questions,
    Question 2 — resolved: not referenced by the Frontend).
  - Wire the Frontend's Backend `HttpClient` to resolve the Backend's address from a static,
    pinned-port configuration value, rather than a code-level hardcoded URL (see Open Questions,
    Question 1).
  - Give the Backend a CORS policy that allows the Frontend's orchestrated origin, since they
    run as two separate origins/ports.
  - Preserve the ability to run Backend and Frontend independently (`dotnet run` in each project,
    or via existing `launchSettings.json` profiles) for developers who don't want the AppHost —
    Aspire should be additive, not a hard requirement to build/run each project standalone.
  - Keep `SystemShogun.slnx` up to date with the two new projects.
- Non-Goals:
  - Deploying the AppHost/Aspire manifest to Azure via `azd` or wiring Aspire into
    `infra/` — this repo has no `infra/` content yet and Azure deployment of Aspire-orchestrated
    services is a separate future spec.
  - Adding new backing services (databases, caches, message queues) to the AppHost. This spec
    only orchestrates the two existing projects.
  - Production/non-local use of the Aspire dashboard or telemetry export destinations (e.g.
    wiring OpenTelemetry to an actual Azure Monitor/App Insights backend) — `ServiceDefaults`
    will set up the OTel plumbing but exporting only needs to work for local dev via the Aspire
    dashboard in this spec.
  - Implementing any real Backend API endpoints or Frontend pages/components. This spec only
    covers orchestration and wiring; no functional application code changes beyond what's needed
    to prove the wiring works (e.g., CORS policy, `HttpClient` base-address resolution).
  - Central package management (`Directory.Packages.props`) — out of scope; the repo doesn't
    use it today and this spec won't introduce it.

## Requirements

1. New project `src/AppHost/SystemShogun.AppHost.csproj` targeting `net10.0`, using the
   `Aspire.Hosting.AppHost` SDK/package, that:
   - References both Backend and Frontend as orchestrated resources.
   - Configures the Backend and Frontend resources to use their existing fixed
     `launchSettings.json` ports (Backend `https://localhost:7024` / `http://localhost:5258`,
     Frontend `https://localhost:7026` / `http://localhost:5056`) rather than Aspire's default
     dynamic port assignment. See "Resolved" notes under Open Questions for rationale.
   - Wires the Frontend resource to reference the Backend resource so Aspire is aware of the
     dependency for dashboard/orchestration purposes (start/stop ordering, dashboard graph), even
     though the Frontend does not consume Aspire-injected endpoint configuration at runtime (see
     Requirement 4).
   - Runs both with a single `dotnet run --project src/AppHost` (or equivalent) and opens/exposes
     the Aspire dashboard for local dev.
2. New project `src/ServiceDefaults/SystemShogun.ServiceDefaults.csproj` targeting `net10.0`,
   providing an `AddServiceDefaults(this IHostApplicationBuilder builder)` extension (and a
   `MapDefaultEndpoints(this WebApplication app)` extension for the Backend) covering:
   - OpenTelemetry (traces, metrics, logs) configured to export to the Aspire dashboard locally.
   - Default health-check endpoints (`/health`, `/alive`).
   - Default `HttpClient` service discovery + standard resilience handler registration.
3. Backend (`SystemShogun.Backend.csproj`):
   - Takes a project reference to `SystemShogun.ServiceDefaults`.
   - Calls `builder.AddServiceDefaults()` and `app.MapDefaultEndpoints()` in `Program.cs`.
   - Adds a CORS policy permitting the Frontend's orchestrated origin(s), applied to any mapped
     endpoints.
4. Frontend (`SystemShogun.Frontend.csproj`):
   - Its Backend-facing `HttpClient` resolves the Backend's base address from a single static
     config value in `wwwroot/appsettings.Development.json` (new file), read the same way whether
     the Frontend is running standalone or under the AppHost. This is correct in both modes
     because the AppHost pins the Backend to its existing fixed port (Requirement 1) — the
     Backend's address never changes between run modes, so there is no separate
     Aspire-injected-configuration code path to implement.
   - No hardcoded backend port numbers in `Program.cs` (the URL lives in configuration, not code).
5. `SystemShogun.slnx` is updated to include the two new projects (and their folders, consistent
   with the existing `/src/Backend/` and `/src/Frontend/` folder pattern).
6. Running Backend and Frontend independently (outside the AppHost, as today) must still work
   without errors — the AppHost is an additional way to run them, not a replacement.
7. `.gitignore` is updated if Aspire tooling produces additional local artifacts that shouldn't
   be committed (e.g., Aspire manifest output if generated locally), consistent with the existing
   `bin/`/`obj/` exclusions.

## Constraints & Assumptions

- Target framework across the repo is `net10.0`; the new projects must match.
- The Backend uses the `Microsoft.NET.Sdk.Web` SDK with only `Microsoft.AspNetCore.OpenApi`
  referenced today — no controllers/minimal API endpoints exist yet beyond `/openapi`.
- The Frontend is a **standalone** Blazor WebAssembly app (`Microsoft.NET.Sdk.BlazorWebAssembly`),
  not Blazor Server / hosted WebAssembly. This matters because standalone WASM code runs
  entirely inside the browser sandbox: it cannot read the host process's environment variables
  the way a normal .NET Aspire-orchestrated project can. Aspire's `AddServiceDefaults`/service
  discovery patterns are designed around `IHostApplicationBuilder`, which
  `WebAssemblyHostBuilder` does not implement, so the Frontend cannot simply call
  `builder.AddServiceDefaults()` the way the Backend does. See Open Questions for how the
  Backend's endpoint should actually reach the browser-side app.
- No `infra/` content exists yet in this repo (confirmed empty), and there's no existing CI/CD
  pipeline referencing the current two projects that this change needs to keep working beyond
  local `dotnet build`/`dotnet run`.
- No central package management file exists; new packages are added via per-project
  `PackageReference`s, consistent with the existing two `.csproj` files.
- Assumes the Aspire workload/SDK version compatible with `.NET 10` and the currently referenced
  ASP.NET Core `10.0.11` package line is used; exact package versions are an implementation
  detail for the `backend`/`frontend` agents at implementation time, not fixed by this spec.

## Proposed Architecture

Two new projects are added under `src/`, alongside the existing `Backend` and `Frontend`
folders, following the same solution-folder convention already used in `SystemShogun.slnx`:

```
src/
  AppHost/            (new) SystemShogun.AppHost.csproj
  ServiceDefaults/    (new) SystemShogun.ServiceDefaults.csproj
  Backend/            SystemShogun.Backend.csproj (existing, updated)
  Frontend/           SystemShogun.Frontend.csproj (existing, updated)
```

`AppHost` is the distributed application entry point: its `Program.cs` builds a
`DistributedApplicationBuilder`, adds the Backend as a project resource pinned to its existing
fixed ports, adds the Frontend as a project resource likewise pinned to its existing fixed ports,
wires a reference from Frontend to Backend so Aspire is aware of the dependency for
dashboard/orchestration purposes, and runs the app graph. Developers use
`dotnet run --project src/AppHost` (or the equivalent IDE launch profile) as the new "run
everything" entry point; the Aspire dashboard comes up alongside it showing both resources'
logs/traces/health.

`ServiceDefaults` is a shared class library referenced only by the Backend (see Open Questions,
Question 2 — resolved) providing the common `AddServiceDefaults`/`MapDefaultEndpoints`
extensions: OpenTelemetry wiring, health-check endpoints, and default resilient `HttpClient`
configuration via `Microsoft.Extensions.ServiceDiscovery`.

### Backend

- Add project reference to `SystemShogun.ServiceDefaults`.
- `Program.cs`: call `builder.AddServiceDefaults()` right after `WebApplication.CreateBuilder`,
  and `app.MapDefaultEndpoints()` before `app.Run()`, per the standard Aspire template shape.
- Add a named CORS policy (e.g. `"Frontend"`) permitting the Frontend's orchestrated origin(s),
  registered via `builder.Services.AddCors(...)` and applied with `app.UseCors("Frontend")`. Since
  both the AppHost and standalone run modes pin the Frontend to its existing
  `launchSettings.json` ports (`https://localhost:7026`, `http://localhost:5056` — see Open
  Questions), the allowed origin is the same fixed value in both modes; no per-mode branching is
  needed. Exact mechanism for keeping the allowed-origin list correct is an implementation
  detail, but must not require editing C# every time a port changes — prefer reading allowed
  origins from configuration.
- No new business endpoints are added by this spec; existing `/openapi` mapping is untouched
  aside from `MapDefaultEndpoints()` adding `/health` and `/alive`.

### Frontend

- `Program.cs` changes to resolve the Backend's `HttpClient` base address from a single
  configuration value (e.g. a `"BackendUrl"` key) read from `wwwroot/appsettings.Development.json`
  (new file), instead of always using `builder.HostEnvironment.BaseAddress`. This same
  configuration value is used whether the Frontend is running standalone or under the AppHost —
  there is no Aspire-injected-configuration code path, and no fallback branching, because the
  Backend's address is pinned (Requirement 1) and therefore identical in both modes.
- `ServiceDefaults` is not referenced by the Frontend project (see Open Questions, resolved). The
  Frontend is still addable as an Aspire resource from the AppHost for dashboard/orchestration
  purposes; it just doesn't consume `AddServiceDefaults()` or any WASM-side discovery helper.

## Trade-offs & Alternatives Considered

- **Alternative: keep two hardcoded `launchSettings.json` ports and just document "run both
  manually."** Rejected — this is the status quo and the whole point of this spec is to remove
  that manual coordination as the app grows past two trivial projects.
- **Alternative: convert Frontend to hosted Blazor WebAssembly (served by Backend) instead of
  standalone, sidestepping the browser-sandbox config problem entirely.** Rejected for this
  spec — it's a bigger architectural change than "add orchestration," would need its own spec,
  and the request was specifically to orchestrate the existing Backend + Frontend projects as
  they are.
- **Alternative: use a `docker-compose.yml` instead of Aspire.** Rejected per the explicit
  request for .NET Aspire, and because Aspire gives .NET-native local dashboard/telemetry with
  less infrastructure to maintain than a Compose file for two .NET dev-server processes that
  aren't containerized today.
- **Alternative: true dynamic browser-side service discovery** — e.g. the Frontend's dev-server
  process serving a dynamic config endpoint the WASM app fetches at startup, or templating a
  `<meta>` tag into `index.html` with the resolved Backend URL at dev-server startup. Considered
  and rejected in favor of pinned ports (see Open Questions, Question 1) — for a 2-service repo,
  the added moving parts aren't justified by the outcome, which pinned ports already achieve.

## Risks

- **Browser-sandbox limitation for standalone WASM — resolved by design, not mitigated at
  runtime.** Aspire's service discovery and `ServiceDefaults` pattern assumes a normal .NET host
  process reading its own environment variables; standalone Blazor WASM's actual `Program.cs`
  runs in the browser and cannot do that. Rather than building a workaround to bridge Aspire's
  env-var-based discovery into the browser (a dynamic config endpoint or templated `index.html`
  meta tag), this spec avoids the problem entirely: the AppHost pins the Backend (and Frontend)
  to their existing fixed ports, so the Frontend's Backend `HttpClient` can use one static config
  value (`wwwroot/appsettings.Development.json`) that's correct in both AppHost-orchestrated and
  standalone modes. No dynamic discovery code path exists to break. If the app grows beyond two
  services and pinned ports stop scaling, true dynamic browser-side discovery may need
  revisiting in a future spec — but that's explicitly a non-goal here.
- **CORS misconfiguration.** Since Backend and Frontend are different origins under Aspire
  (different ports), a missing/incorrect CORS policy will silently break every future Frontend
  call to the Backend. Mitigation: cover this explicitly in the Testing Plan.
- **Divergence between AppHost-orchestrated and standalone run modes.** Because the pinned-port
  approach relies on the same values being kept in sync across three places —
  `launchSettings.json`, the AppHost's port configuration, and the Frontend's
  `wwwroot/appsettings.Development.json` — if someone changes a port in one place without the
  others, either the AppHost or standalone mode silently breaks. Mitigation: keep the values
  colocated/commented so they're easy to keep in sync, and cover both run modes in the Testing
  Plan.
- **Aspire workload/tooling version drift.** Aspire's SDK and package versions move quickly;
  pinning to versions incompatible with `net10.0` could break `dotnet build`. Mitigation: the
  `backend`/`frontend` implementers should use the latest Aspire release compatible with the
  repo's current `net10.0`/ASP.NET Core `10.0.11` line at implementation time.

## Open Questions

All five questions below have been resolved by the approver. They're kept here (with the
decision and rationale) rather than deleted, so implementers have the full history and don't
need to re-derive "why" from the rest of the spec alone.

1. **How does the Backend's endpoint reach the browser?** — **Resolved: option (c), pinned
   ports.** The AppHost pins the Backend to its existing `launchSettings.json` ports
   (`https://localhost:7024`, `http://localhost:5258`) instead of letting Aspire auto-assign
   dynamic ports. The Frontend's Backend-facing `HttpClient` resolves its base address from a
   static config value in `wwwroot/appsettings.Development.json` — the same value whether running
   standalone or under the AppHost, since the Backend's port never changes between modes. This
   avoids bridging Aspire's env-var-based service discovery into the browser sandbox at all (which
   standalone `WebAssemblyHostBuilder` fundamentally cannot consume without a custom dev-server
   endpoint or build-time templating). Rationale: this repo's CLAUDE.md discourages building
   abstractions/pipelines beyond what's needed — a config-bridging endpoint or templated meta tag
   is meaningfully more moving parts than a pinned port + static JSON file for a 2-service repo,
   for the same practical outcome. Aspire still delivers on the spec's actual goals: single-command
   startup, unified dashboard, telemetry/health on the Backend, and CORS wiring.
2. **Should `ServiceDefaults` be referenced by the Frontend?** — **Resolved: no.**
   `ServiceDefaults` is referenced only by the Backend project. Because Question 1 is resolved via
   port-pinning rather than dynamic discovery, there's no WASM-side discovery/config helper
   needed, so there's nothing in `ServiceDefaults` for the Frontend to consume — not even a
   lighter subset.
3. **Naming conventions** — **Resolved: approved as originally proposed.**
   `SystemShogun.AppHost` and `SystemShogun.ServiceDefaults`, consistent with the existing
   `SystemShogun.Backend` / `SystemShogun.Frontend` naming pattern.
4. **Port conventions under Aspire** — **Resolved: pin existing ports for both services.**
   Following the Question 1 decision, the AppHost pins the Backend to its existing ports
   (`7024`/`5258`) rather than letting Aspire auto-assign. The Frontend is likewise pinned to its
   existing ports (`https://localhost:7026`, `http://localhost:5056`) when run under the AppHost,
   so the Backend's CORS allowed-origin is a fixed, known value that doesn't need to differ
   between AppHost-orchestrated and standalone modes.
5. **Should this spec touch CI?** — **Resolved: confirmed out of scope**, as originally proposed.
   No existing CI pipeline exists in the repo to extend.

## Testing Plan

- `dotnet build` succeeds for the full solution (`SystemShogun.slnx`), including the two new
  projects.
- `dotnet run --project src/AppHost` starts both Backend and Frontend, the Aspire dashboard is
  reachable locally, and both resources show as healthy (`/health`/`/alive` on the Backend).
- With the AppHost running, a manual smoke test (e.g. a temporary `fetch`/`HttpClient` call, or
  browser dev-tools network tab against a simple existing endpoint like `/health`) confirms the
  Frontend can successfully reach the Backend across origins without a CORS error, using the
  Aspire-resolved address rather than a hardcoded one.
- `dotnet run` in `src/Backend` alone, and `dotnet run` in `src/Frontend` alone (existing
  `launchSettings.json` profiles), both still work exactly as before this change, proving the
  AppHost is additive and non-breaking.
- Manual verification that stopping the AppHost cleanly stops both child processes (no orphaned
  `dotnet` processes left running on the configured ports).
