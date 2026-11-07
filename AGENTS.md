# AGENTS.md

The RedPand MCP server: a C# ASP.NET Core MCP server for the RedPand MCP Unity plugin
(https://github.com/red-panda-gamedev/RedPand-MCP, private). A thin host (`src/Program.cs`) over the plugin
library in `lib/` (built from source here) and the NuGet package `com.IvanMurzak.ReflectorNet`. Distributed as
standalone executables (`redpand-mcp-server-<rid>.zip` on GitHub Releases) and a Docker image
(`ghcr.io/red-panda-gamedev/redpand-mcp-server`). No NuGet tool.

This repository is public and Apache-2.0 only. Keep the original author's copyright headers, `LICENSE`,
`NOTICE` and `lib/NOTICE`. Never push to `IvanMurzak/*` (the `upstream` remote has push disabled).

## Build / run

```bash
dotnet build com.RedPand.MCP.Server.csproj
dotnet run --project com.RedPand.MCP.Server.csproj -- --client-transport stdio --port 8080
```

- All-platform self-contained publish: `python build/build_all.py [Debug|Release] [rid ...]` (stdlib
  Python 3.9+, any OS; `--no-zip`, then `--zip-only` to zip after an external signing step).
- Tests: `dotnet test tests/GameDev.MCP.Server.Tests/GameDev.MCP.Server.Tests.csproj`,
  `dotnet test lib/McpPlugin.Tests/McpPlugin.Tests.csproj`, `dotnet test lib/McpPlugin.Server.Tests/McpPlugin.Server.Tests.csproj`,
  `python -m unittest discover -s scripts/tests` (set `PYTHONUTF8=1` on Windows).

## Layout

- `src/` — the host: `Program.cs`, startup/auth defaults (`Startup/`), the `configure` command (`Configure/`).
- `lib/` — the plugin library (`com.RedPand.MCP.Plugin*`): McpPlugin, .Common, .Server and their tests.
  The Unity plugin ships DLLs built from it (`commands/plugin_library.py` in the plugin repo).
- `tests/GameDev.MCP.Server.Tests/` — xUnit tests of the host (incl. `NoHostedCloudTests`: no `ai-game.dev`).
- `build/` — `build_all.py` and the macOS entitlements.
- `scripts/` — the release dependency guard and its offline unit tests (`scripts/tests/`).
- `.github/workflows/` — `test_pull_request.yml` (PR gate) and the tag-triggered `release.yml` with its children.
- Root: the csproj, `server.json` (MCP registry listing `io.github.red-panda-gamedev/redpand-mcp`), `Dockerfile`.

## Versioning / releases

- `<Version>` in the csproj, `server.json`'s `version` and its OCI identifier tag move together.
- The Unity plugin pins the server by the `ServerVersion` constant in its `McpServerManager.cs`; a release
  with all 7 zips must exist before a plugin release pins it.
- **Release = push tag `v<version>`.** `release.yml`: guard (dependency guard; tag == `v<csproj Version>`;
  the GitHub Release must not exist) -> full tests (`test_pull_request.yml`) -> executables (unsigned unless
  the optional macOS signing secrets are set) + Docker image to GHCR (`GITHUB_TOKEN`) -> GitHub Release
  (7 zips + `SHA256SUMS`) -> verify. A manual dispatch is a dry run that publishes nothing.
- A new GHCR package starts private: make it public once in the org's package settings.
