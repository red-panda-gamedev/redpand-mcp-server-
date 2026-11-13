# RedPand MCP Server

The [Model Context Protocol](https://modelcontextprotocol.io/) server for **RedPand MCP**. It lets AI agents
(Claude, Cursor, Copilot, Codex, …) drive the Unity Editor through the RedPand MCP plugin:

```
MCP client  ⇄  redpand-mcp-server  ⇄ (SignalR) ⇄  RedPand MCP plugin (Unity Editor / game)
```

The server holds no engine-specific code. Tools, resources and prompts come from the plugin that connects.
The plugin library (`lib/`) is built from source in this repository.

## Install / Run

The RedPand MCP plugin downloads and starts the server for you. To run it by hand:

### Pre-built executables

Download the zip for your platform from [Releases](https://github.com/red-panda-gamedev/redpand-mcp-server-/releases)
(`redpand-mcp-server-<rid>.zip`, where `<rid>` is `win-x64`, `win-x86`, `win-arm64`, `linux-x64`, `linux-arm64`,
`osx-x64` or `osx-arm64`). Each release carries a `SHA256SUMS` file to check the zips against.

```bash
./redpand-mcp-server --port 8080 --client-transport stdio
```

MCP client config (stdio):

```json
{
  "mcpServers": {
    "redpand": {
      "command": "path/to/redpand-mcp-server",
      "args": ["--port=8080", "--client-transport=stdio"]
    }
  }
}
```

The executables are not code-signed. On macOS you may need to allow the binary in System Settings → Privacy & Security.

### Docker

```bash
docker run -i --rm -p 8080:8080 ghcr.io/red-panda-gamedev/redpand-mcp-server
```

### From source

```bash
dotnet build com.RedPand.MCP.Server.csproj
dotnet run --project com.RedPand.MCP.Server.csproj -- --client-transport stdio --port 8080
```

Self-contained executables for all 7 platforms: `python build/build_all.py` (Python 3.9+, any OS; pass RIDs to build
only some). Outputs land in `publish/<rid>/` and are zipped as `publish/redpand-mcp-server-<rid>.zip`.

## Configuration

CLI arguments override environment variables.

| Environment variable | CLI argument | Default | Description |
| --- | --- | --- | --- |
| `MCP_PLUGIN_PORT` | `--port` | `8080` | Client → Server ← Plugin connection port |
| `MCP_PLUGIN_CLIENT_TIMEOUT` | `--plugin-timeout` | `10000` | One attempt of a catalog / prompt / resource request to the plugin (ms). Tool calls use `--tool-timeout` |
| `MCP_PLUGIN_TOOL_TIMEOUT` | `--tool-timeout` | `300000` (or `--plugin-timeout` if larger) | How long one tool call may run in the plugin (ms). A tool call is sent once and never re-sent |
| `MCP_PLUGIN_CLIENT_TRANSPORT` | `--client-transport` | `stdio` | Client → Server transport: `stdio` or `streamableHttp` |
| `MCP_PLUGIN_IDLE_TIMEOUT_SECONDS` | `--idle-timeout-seconds` | `28800` | streamableHttp idle-session eviction window |
| `MCP_AUTH` | `--auth` | `none` | Authentication mode: `none` or `token` (see [Authentication](#authentication)) |
| `MCP_PLUGIN_TOKEN` | `--token` | — | The shared secret for `token` mode |
| `MCP_PUBLIC_URL` | `--public-url` | — | This server's public URL; its origin is allowed for browser clients |
| `MCP_BIND` | `--bind` | `loopback` | Bind address: `loopback`, `any` (0.0.0.0), or a specific IP |
| `MCP_ALLOWED_ORIGINS` | `--allowed-origins` | — | Comma/semicolon-separated additional allowed browser Origins |
| `MCP_SENTRY_DSN` / `MCP_SENTRY_ENVIRONMENT` | — | — | Opt-in crash reporting to a Sentry-compatible endpoint: unhandled exceptions only, no PII. Unset = off |
| `MCP_LOG_LEVEL` | — | `Info` | Log verbosity floor: `Trace`, `Debug`, `Info`, `Warn`, `Error`, `Fatal` |

Logs are written to `logs/server-log.txt` (and `logs/server-log-error.txt`). In stdio mode console logging goes to
stderr, so stdout stays clean for the MCP JSON stream.

## Authentication

| Mode | What it does | Use |
| --- | --- | --- |
| `none` | no authentication | local work (default) |
| `token` | the plugin connection and the HTTP endpoint require the shared secret `--token` / `MCP_PLUGIN_TOKEN` | a server reachable from other machines |

- `required` is accepted as an old name for `token`. The hosted-cloud `oauth` mode was removed; it is refused at startup.
- Origin validation (DNS-rebinding defense) runs in every mode: a request with a non-allowed `Origin` gets `403`.
  Loopback and the `--public-url` origin are allowed; add more with `MCP_ALLOWED_ORIGINS`.
- Bind defaults to loopback. Set `--bind any` only together with `--auth token`.

```bash
# Local (stdio, no auth):
redpand-mcp-server --client-transport stdio --port 8080

# Reachable over the network (streamableHttp, token):
redpand-mcp-server --client-transport streamableHttp --port 8080 --auth token --token <secret> --bind any
```

## Configure an AI agent

Write a project-scoped MCP client config for a supported AI agent from the terminal:

```bash
redpand-mcp-server configure --agent claude-code
redpand-mcp-server configure --help   # lists the agents
```

`--transport stdio` writes a stdio spawn config instead of an HTTP one; `--project <path>` targets another project
root (default: the current directory).

## Releases

Push a tag `v<version>` matching the `<Version>` in `com.RedPand.MCP.Server.csproj` (and `server.json`).
`.github/workflows/release.yml` then runs the tests and publishes the 7 zips with `SHA256SUMS` on the GitHub
Release and the image `ghcr.io/red-panda-gamedev/redpand-mcp-server:<version>`.

## Security

To report a vulnerability, see [SECURITY.md](SECURITY.md).

## License

[Apache-2.0](LICENSE). Based on [GameDev-MCP-Server](https://github.com/IvanMurzak/GameDev-MCP-Server) and
[MCP-Plugin-dotnet](https://github.com/IvanMurzak/MCP-Plugin-dotnet) by Ivan Murzak (Apache-2.0); see [NOTICE](NOTICE)
and [lib/NOTICE](lib/NOTICE).
