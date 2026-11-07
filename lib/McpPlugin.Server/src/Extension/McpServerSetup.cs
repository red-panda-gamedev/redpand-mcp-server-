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
using com.RedPand.MCP.Plugin.Server.Strategy;
using com.RedPand.MCP.Plugin.Server.Transport;

namespace com.RedPand.MCP.Plugin.Server
{
    internal sealed class McpServerSetup
    {
        public IMcpConnectionStrategy Strategy { get; }
        public ITransportLayer Transport { get; }

        public McpServerSetup(IMcpConnectionStrategy strategy, ITransportLayer transport)
        {
            Strategy = strategy ?? throw new ArgumentNullException(nameof(strategy));
            Transport = transport ?? throw new ArgumentNullException(nameof(transport));
        }
    }
}
