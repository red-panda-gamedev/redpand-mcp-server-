/*
┌────────────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)                   │
│  Repository: GitHub (https://github.com/IvanMurzak/MCP-Plugin-dotnet)  │
│  Copyright (c) 2025 Ivan Murzak                                        │
│  Licensed under the Apache License, Version 2.0.                       │
│  See the LICENSE file in the project root for more information.        │
└────────────────────────────────────────────────────────────────────────┘
*/

#nullable enable
using System;
using System.Collections.Generic;
using com.RedPand.MCP.Plugin.Common;
using static com.RedPand.MCP.Plugin.Common.Consts.MCP.Server;

namespace com.RedPand.MCP.Plugin.ServerLaunch
{
    /// <summary>
    /// Builds the launch-argument list an engine plugin passes to the shared MCP server executable
    /// (mcp-authorize g6 consolidation). One canonical builder replaces the per-engine
    /// <c>BuildArguments</c> duplicates: Unity and Godot (C#) call it directly; Unreal's C++ editor
    /// delegates to its .NET sidecar, which calls it too. Emitting the argument shape here — rather than
    /// re-deriving it in three engines — guarantees every engine wires <c>auth</c> and <c>token</c> identically.
    /// <para>
    /// The emitted shape (in order): <c>port</c>, <c>plugin-timeout</c>, <c>client-transport</c>,
    /// <c>auth=&lt;mode&gt;</c> (the target-state key), then per mode:
    /// </para>
    /// <list type="bullet">
    ///   <item><see cref="Consts.MCP.Server.AuthOption.none"/> — no credential args.</item>
    ///   <item><see cref="Consts.MCP.Server.AuthOption.token"/> — <c>token=&lt;secret&gt;</c> (required).</item>
    /// </list>
    /// </summary>
    public static class ServerLaunchArguments
    {
        /// <summary>
        /// Builds the ordered launch arguments (each a <c>key=value</c> token) for the given server
        /// configuration. Throws <see cref="ArgumentException"/> when a mode's required credential is
        /// missing (token mode without a secret) — the same fail-closed contract the server's strategy <c>Validate</c> enforces at boot.
        /// </summary>
        /// <param name="port">The MCP server port.</param>
        /// <param name="pluginTimeoutMs">Plugin connection timeout, milliseconds.</param>
        /// <param name="clientTransport">stdio or streamableHttp.</param>
        /// <param name="authOption">The auth mode: none or token. (oauth was removed with the hosted cloud.)</param>
        /// <param name="token">The shared secret; required in <see cref="Consts.MCP.Server.AuthOption.token"/> mode, ignored otherwise.</param>
        public static IReadOnlyList<string> Build(
            int port,
            int pluginTimeoutMs,
            Consts.MCP.Server.TransportMethod clientTransport,
            Consts.MCP.Server.AuthOption authOption,
            string? token = null)
        {
            var args = new List<string>
            {
                $"{Args.Port}={port}",
                $"{Args.PluginTimeout}={pluginTimeoutMs}",
                $"{Args.ClientTransportMethod}={clientTransport}",
                $"{Args.Auth}={authOption}"
            };

            switch (authOption)
            {
                case Consts.MCP.Server.AuthOption.none:
                    break;

                case Consts.MCP.Server.AuthOption.token:
                    if (string.IsNullOrWhiteSpace(token))
                        throw new ArgumentException($"auth={Consts.MCP.Server.AuthOption.token} requires a non-empty token.", nameof(token));
                    args.Add($"{Args.Token}={token}");
                    break;

                default:
                    // `required` is the deprecated alias resolved server-side; a NEW caller emits the
                    // target-state `token` instead. `unknown` is never a launch target, and `oauth` was removed.
                    throw new ArgumentException(
                        $"Unsupported launch auth option: {authOption}. Use {Consts.MCP.Server.AuthOption.none} or {Consts.MCP.Server.AuthOption.token}.",
                        nameof(authOption));
            }

            return args;
        }

        /// <summary>
        /// Convenience wrapper over <see cref="Build"/> that space-joins the arguments into a single
        /// command-line string (for engines that pass a raw string rather than an argv array). Values
        /// are emitted verbatim; the server's <c>DataArguments</c> parser tokenizes on whitespace, so
        /// callers whose values could contain spaces should prefer the argv-array <see cref="Build"/>.
        /// </summary>
        public static string BuildCommandLine(
            int port,
            int pluginTimeoutMs,
            Consts.MCP.Server.TransportMethod clientTransport,
            Consts.MCP.Server.AuthOption authOption,
            string? token = null)
            => string.Join(" ", Build(port, pluginTimeoutMs, clientTransport, authOption, token));
    }
}
