# Security policy

## Supported versions

Security fixes go into the latest [release](https://github.com/red-panda-gamedev/redpand-mcp-server-/releases)
only. Please check that the issue still happens on it before you report.

## Reporting a vulnerability

Please do not open a public issue. Report privately through GitHub:
**[Security → Report a vulnerability](https://github.com/red-panda-gamedev/redpand-mcp-server-/security/advisories/new)**.

Include:

- the server version (or commit) and the platform (`win-x64`, `linux-arm64`, Docker, …);
- the client transport (`stdio` or `streamableHttp`), the auth mode and the bind address;
- steps to reproduce, and what an attacker gains.

The fix ships as a new release, and the advisory is published once that release is out.

## Scope

In scope: this repository (the server in `src/` and the plugin library in `lib/`), the release executables and the
Docker image `ghcr.io/red-panda-gamedev/redpand-mcp-server`. Issues in the RedPand MCP Unity plugin can be
reported the same way.

Defaults the server relies on:

- It binds to loopback. `--bind any` is meant only together with `--auth token`; running `--bind any --auth none`
  on an untrusted network is a misconfiguration, not a vulnerability.
- Requests with a non-allowed `Origin` are refused with `403` in every auth mode (DNS-rebinding defense).
- Every build runs a NuGet audit and fails on any known-vulnerable package, direct or transitive, at moderate
  severity or above.
