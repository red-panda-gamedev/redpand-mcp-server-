/*
┌────────────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)                   │
│  Repository: GitHub (https://github.com/IvanMurzak/MCP-Plugin-dotnet)  │
│  Copyright (c) 2025 Ivan Murzak                                        │
│  Licensed under the Apache License, Version 2.0.                       │
│  See the LICENSE file in the project root for more information.        │
└────────────────────────────────────────────────────────────────────────┘
*/
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace com.RedPand.MCP.Plugin.Common.Model
{
    public class RequestCallTool : IRequestID, IDisposable
    {
        public string RequestID { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public IReadOnlyDictionary<string, JsonElement> Arguments { get; set; } = new Dictionary<string, JsonElement>();

        /// <summary>
        /// Who sent the call: the agent session's short label (client name and a hash of the MCP session), set by the
        /// server. Null from a server that does not set it. Lets an editor shared by several agents tell them apart.
        /// </summary>
        public string? Caller { get; set; }

        public RequestCallTool() { }
        public RequestCallTool(string name, IReadOnlyDictionary<string, JsonElement> arguments)
            : this(Guid.NewGuid().ToString(), name, arguments) { }
        public RequestCallTool(string requestId, string name, IReadOnlyDictionary<string, JsonElement> arguments)
        {
            RequestID = requestId ?? throw new ArgumentNullException(nameof(requestId));
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Arguments = arguments ?? throw new ArgumentNullException(nameof(arguments));
        }

        public virtual void Dispose()
        {
            // Arguments.Clear();
        }
        ~RequestCallTool() => Dispose();
    }
}
