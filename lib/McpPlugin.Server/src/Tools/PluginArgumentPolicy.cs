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
using System.Collections.Concurrent;

namespace com.RedPand.MCP.Plugin.Server.Tools
{
    /// <summary>
    /// Which plugin connections get the server-side tool-argument validation (<see cref="ToolArgumentValidator"/>).
    /// <para>
    /// A plugin built on McpPlugin before 8.14.0 (ReflectorNet before 5.5.1) lists every parameter without a
    /// default as <c>required</c>, while its binder fills an omitted or null one with null/0, and its binder may
    /// accept values the schema check would refuse. Server 9.2.x validated nothing, so only a connection whose
    /// handshake reports <c>mcpPluginVersion</c> 8.14.0 or newer is validated; an older plugin, or one with no or
    /// an unparsable version, gets its calls forwarded exactly as before. Keyed by SignalR connection id:
    /// recorded at the accepted version handshake, forgotten when the connection closes.
    /// </para>
    /// </summary>
    public static class PluginArgumentPolicy
    {
        /// <summary>The first McpPlugin version whose <c>required</c> lists match what its binder needs.</summary>
        public static readonly System.Version FirstEnforcedVersion = new System.Version(8, 14, 0);

        /// <summary>
        /// The first McpPlugin version that keeps a journal of the calls it ran: a call sent again with the same request
        /// id is answered with the first run's result, so the server may resend any call lost to a reload.
        /// </summary>
        public static readonly System.Version FirstJournaledVersion = new System.Version(8, 20, 0);

        static readonly ConcurrentDictionary<string, bool> Enforced = new ConcurrentDictionary<string, bool>(StringComparer.Ordinal);
        static readonly ConcurrentDictionary<string, bool> Journaled = new ConcurrentDictionary<string, bool>(StringComparer.Ordinal);

        /// <summary>Records the McpPlugin version a connection's handshake reported (null when it reported none).</summary>
        public static void Record(string connectionId, string? mcpPluginVersion)
        {
            if (string.IsNullOrEmpty(connectionId))
                return;
            Enforced[connectionId] = EnforcesRequired(mcpPluginVersion);
            Journaled[connectionId] = AtLeast(mcpPluginVersion, FirstJournaledVersion);
        }

        /// <summary>Forgets a closed connection.</summary>
        public static void Forget(string connectionId)
        {
            if (string.IsNullOrEmpty(connectionId))
                return;
            Enforced.TryRemove(connectionId, out _);
            Journaled.TryRemove(connectionId, out _);
        }

        /// <summary>Whether the plugin on <paramref name="connectionId"/> answers a resent call from its journal (8.20.0+).</summary>
        public static bool IsJournaled(string? connectionId)
            => connectionId != null && Journaled.TryGetValue(connectionId, out var journaled) && journaled;

        /// <summary>
        /// Whether calls to <paramref name="connectionId"/> are validated: only after a handshake
        /// that reported McpPlugin 8.14.0 or newer. An unknown connection keeps the lenient (previous) behaviour.
        /// </summary>
        public static bool IsEnforced(string? connectionId)
            => connectionId != null && Enforced.TryGetValue(connectionId, out var enforced) && enforced;

        /// <summary>
        /// True when <paramref name="mcpPluginVersion"/> is 8.14.0 or newer. A pre-release or build suffix is
        /// ignored (<c>8.14.0-dev.x</c> counts as 8.14.0); null, empty or unparsable is false.
        /// </summary>
        public static bool EnforcesRequired(string? mcpPluginVersion) => AtLeast(mcpPluginVersion, FirstEnforcedVersion);

        /// <summary>True when <paramref name="mcpPluginVersion"/> is <paramref name="first"/> or newer (a suffix is ignored).</summary>
        public static bool AtLeast(string? mcpPluginVersion, System.Version first)
        {
            if (string.IsNullOrWhiteSpace(mcpPluginVersion))
                return false;
            var text = mcpPluginVersion!.Trim();
            var cut = text.IndexOfAny(new[] { '-', '+' });
            if (cut >= 0)
                text = text.Substring(0, cut);
            return System.Version.TryParse(text, out var version)
                && new System.Version(version.Major, version.Minor, Math.Max(version.Build, 0)) >= first;
        }
    }
}
