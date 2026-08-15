var builder = DistributedApplication.CreateBuilder(args);

// Backend and Frontend are pinned to their existing Properties/launchSettings.json ports
// (Backend: https://localhost:7024 / http://localhost:5258; Frontend: https://localhost:7026 /
// http://localhost:5056) rather than Aspire's default dynamic port assignment. AddProject uses
// each project's "https" launch profile ports by default, so no explicit endpoint configuration
// is needed here - launchSettings.json stays the single source of truth for local ports instead
// of duplicating them in this file too. If a port ever changes, update it there and both the
// AppHost and standalone `dotnet run` stay in sync automatically.
var backend = builder.AddProject<Projects.SystemShogun_Backend>("backend")
    .WithHttpHealthCheck("/health");

// The Frontend references the Backend purely for Aspire dashboard/orchestration awareness
// (start/stop ordering, dependency graph) - the Frontend does not consume Aspire-injected
// configuration at runtime, since standalone Blazor WebAssembly runs in the browser sandbox and
// cannot read the host process's environment variables. Its Backend HttpClient instead resolves
// the Backend's address from a static config value (see Frontend's wwwroot/appsettings.Development.json).
builder.AddProject<Projects.SystemShogun_Frontend>("frontend")
    .WithExternalHttpEndpoints()
    .WithReference(backend)
    .WaitFor(backend);

builder.Build().Run();
