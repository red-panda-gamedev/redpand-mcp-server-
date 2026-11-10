/*
┌────────────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)                   │
│  Repository: GitHub (https://github.com/IvanMurzak/MCP-Plugin-dotnet)  │
│  Copyright (c) 2025 Ivan Murzak                                        │
│  Licensed under the Apache License, Version 2.0.                       │
│  See the LICENSE file in the project root for more information.        │
└────────────────────────────────────────────────────────────────────────┘
*/

using com.RedPand.MCP.Plugin.Common.Hub.Server;
using com.RedPand.MCP.Plugin.Common.Model;

namespace com.RedPand.MCP.Plugin
{
    public interface IMcpManagerHub : IConnectServerHub, IServerMcpManager
    {
        /// <summary>
        /// Gets the version handshake response status if a handshake has been performed; otherwise, null.
        /// </summary>
        VersionHandshakeResponse? VersionHandshakeStatus { get; }

        /// <summary>
        /// Tells the server what the editor is doing (<see cref="IServerEditorStateHub"/>) while connected; never throws,
        /// and a server without the method is not asked again on this connection. Hubs that cannot send it ignore it.
        /// </summary>
        System.Threading.Tasks.Task NotifyEditorState(RequestEditorState request) => System.Threading.Tasks.Task.CompletedTask;
    }
}
