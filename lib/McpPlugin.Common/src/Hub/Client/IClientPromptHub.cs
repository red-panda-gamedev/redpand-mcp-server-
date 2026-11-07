/*
┌────────────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)                   │
│  Repository: GitHub (https://github.com/IvanMurzak/MCP-Plugin-dotnet)  │
│  Copyright (c) 2025 Ivan Murzak                                        │
│  Licensed under the Apache License, Version 2.0.                       │
│  See the LICENSE file in the project root for more information.        │
└────────────────────────────────────────────────────────────────────────┘
*/

using System.Threading;
using System.Threading.Tasks;
using com.RedPand.MCP.Plugin.Common.Model;

namespace com.RedPand.MCP.Plugin.Common.Hub.Client
{
    public interface IClientPromptHub
    {
        Task<ResponseData<ResponseGetPrompt>> RunGetPrompt(RequestGetPrompt request);

        // Takes a CancellationToken (defaulted, so existing token-less call sites keep compiling) for
        // the same reason IClientToolHub.RunListTool does — see IClientResourceHub.
        Task<ResponseData<ResponseListPrompts>> RunListPrompts(RequestListPrompts request, CancellationToken cancellationToken = default);
    }
}
