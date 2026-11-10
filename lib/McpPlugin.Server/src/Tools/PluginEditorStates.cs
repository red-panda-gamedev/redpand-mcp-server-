// RedPand MCP. Licensed under the Apache License, Version 2.0.
#nullable enable
using System;
using System.Collections.Concurrent;
using System.Globalization;
using com.RedPand.MCP.Plugin.Common.Model;

namespace com.RedPand.MCP.Plugin.Server.Tools
{
    /// <summary>
    /// What each connected editor last said it was doing (<see cref="RequestEditorState"/>), and the last editor that
    /// closed. A call is not left waiting for an editor that closed, and a timeout says what kept the editor busy.
    /// Keyed by SignalR connection id; a plugin before McpPlugin 8.20.0 sends nothing, so nothing changes for it.
    /// </summary>
    public static class PluginEditorStates
    {
        /// <summary>An editor that announced it was closing is taken as gone for this long after it disconnected.</summary>
        public static TimeSpan ClosedMemory { get; set; } = TimeSpan.FromMinutes(30);

        static readonly ConcurrentDictionary<string, RequestEditorState> States = new ConcurrentDictionary<string, RequestEditorState>(StringComparer.Ordinal);
        static Closed? _lastClosed;

        sealed class Closed
        {
            public Closed(DateTime atUtc, string? detail) { AtUtc = atUtc; Detail = detail; }
            public DateTime AtUtc { get; }
            public string? Detail { get; }
        }

        /// <summary>Records a state the plugin on <paramref name="connectionId"/> sent.</summary>
        public static void Record(string connectionId, RequestEditorState? state)
        {
            if (string.IsNullOrEmpty(connectionId) || state == null || !RequestEditorState.IsKnown(state.State))
                return;
            States[connectionId] = state;
        }

        /// <summary>A plugin connected: whatever closed before is back (or replaced).</summary>
        public static void OnConnected(string connectionId)
        {
            _lastClosed = null;
            if (!string.IsNullOrEmpty(connectionId))
                States.TryRemove(connectionId, out _);
        }

        /// <summary>A plugin disconnected: when it had said it was closing, it is remembered as closed.</summary>
        public static void OnDisconnected(string connectionId, DateTime? utcNow = null)
        {
            if (string.IsNullOrEmpty(connectionId) || !States.TryRemove(connectionId, out var last))
                return;
            if (last.State == RequestEditorState.Closing)
                _lastClosed = new Closed(utcNow ?? DateTime.UtcNow, last.Detail);
        }

        /// <summary>The last state <paramref name="connectionId"/> sent, or null.</summary>
        public static RequestEditorState? Get(string? connectionId)
            => connectionId != null && States.TryGetValue(connectionId, out var state) ? state : null;

        /// <summary>True while <paramref name="connectionId"/> has said it is closing (it may not have disconnected yet).</summary>
        public static bool IsClosing(string? connectionId) => Get(connectionId)?.State == RequestEditorState.Closing;

        /// <summary>
        /// The answer for a call that finds no editor because the last one closed (not a reload), or null when no editor
        /// closed recently: then the call waits for a reconnecting plugin as before.
        /// </summary>
        public static string? ClosedMessage(string toolName, DateTime? utcNow = null)
        {
            var closed = _lastClosed;
            var now = utcNow ?? DateTime.UtcNow;
            if (closed == null || now - closed.AtUtc > ClosedMemory)
                return null;
            return $"{ToolCallErrors.EditorClosed} The Unity editor was closed {Ago(now - closed.AtUtc)} ago" +
                   (string.IsNullOrEmpty(closed.Detail) ? string.Empty : $" ({closed.Detail})") +
                   $" and is not connected, so '{toolName}' was not sent. Open the project in the editor, then call wait and retry.";
        }

        /// <summary>A line for a timeout: what the editor said it was busy with, or null when it said nothing.</summary>
        public static string? BusyNote(string? connectionId, DateTime? utcNow = null)
        {
            var state = Get(connectionId);
            if (state == null || state.State == RequestEditorState.Ready)
                return null;
            var since = Ago((utcNow ?? DateTime.UtcNow) - state.SinceUtc);
            return $"The editor reported '{state.State}' for {since}" + (string.IsNullOrEmpty(state.Detail) ? "." : $": {state.Detail}.");
        }

        static string Ago(TimeSpan span)
        {
            if (span < TimeSpan.Zero)
                span = TimeSpan.Zero;
            return span.TotalSeconds < 90
                ? ((int)span.TotalSeconds).ToString(CultureInfo.InvariantCulture) + " s"
                : ((int)span.TotalMinutes).ToString(CultureInfo.InvariantCulture) + " min";
        }

        /// <summary>For tests: forgets everything.</summary>
        public static void Reset()
        {
            States.Clear();
            _lastClosed = null;
        }
    }
}
