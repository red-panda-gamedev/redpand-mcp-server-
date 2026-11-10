// RedPand MCP. Licensed under the Apache License, Version 2.0.
#nullable enable
using System;

namespace com.RedPand.MCP.Plugin.Common.Model
{
    /// <summary>
    /// What the engine editor is doing, sent by the plugin when it changes: the server answers calls with it instead of
    /// waiting blind (a closing editor is not waited for; a timeout names what kept the editor busy).
    /// </summary>
    public class RequestEditorState : IRequestID
    {
        /// <summary>The editor answers calls.</summary>
        public const string Ready = "ready";

        /// <summary>The editor's main thread is blocked (an import, a package resolve, a build); <see cref="Detail"/> says by what.</summary>
        public const string Busy = "busy";

        /// <summary>A script or assembly reload: the plugin disconnects and comes back in seconds.</summary>
        public const string Reloading = "reloading";

        /// <summary>The editor is closing: it will not come back on its own.</summary>
        public const string Closing = "closing";

        public string RequestID { get; set; } = Guid.NewGuid().ToString();

        /// <summary>One of <see cref="Ready"/>, <see cref="Busy"/>, <see cref="Reloading"/>, <see cref="Closing"/>.</summary>
        public string State { get; set; } = Ready;

        /// <summary>What the editor is busy with, e.g. "importing Assets/Hero.fbx (12 of 340)".</summary>
        public string? Detail { get; set; }

        /// <summary>When the state began (UTC).</summary>
        public DateTime SinceUtc { get; set; } = DateTime.UtcNow;

        public static bool IsKnown(string? state)
            => state == Ready || state == Busy || state == Reloading || state == Closing;
    }
}
