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
using System.Threading;
using System.Threading.Tasks;
using com.RedPand.MCP.Plugin.Common.Hub.Client;
using com.RedPand.MCP.Plugin.Common.Model;
using com.RedPand.MCP.Plugin.Common.Utils;
using com.RedPand.MCP.Plugin.Server.Auth;
using com.RedPand.MCP.Plugin.Server.Strategy;
using Microsoft.Extensions.DependencyInjection;

namespace com.RedPand.MCP.Plugin.Server.Tools
{
    /// <summary>
    /// A server-side tool hub whose call takes the caller's token, so a caller that goes away stops
    /// the wait. Not on <see cref="IClientToolHub"/>: the plugin implements that interface too, and
    /// a new member would break it.
    /// </summary>
    internal interface ICancellableToolHub
    {
        Task<ResponseData<ResponseCallTool>> RunCallTool(RequestCallTool request, CancellationToken cancellationToken);
    }

    /// <summary>One dispatch decision for MCP and REST, using the current authenticated request context.</summary>
    internal static class ToolDispatcher
    {
        internal static async Task<ResponseData<ResponseCallTool>?> CallAsync(
            IServiceProvider services, RequestCallTool request, CancellationToken cancellationToken)
        {
            var toolHub = services.GetRequiredService<IClientToolHub>();
            return toolHub is ICancellableToolHub cancellable
                ? await cancellable.RunCallTool(request, cancellationToken)
                : await toolHub.RunCallTool(request);
        }
    }
}
