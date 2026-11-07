/*
┌────────────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)                   │
│  Repository: GitHub (https://github.com/IvanMurzak/MCP-Plugin-dotnet)  │
│  Copyright (c) 2025 Ivan Murzak                                        │
│  Licensed under the Apache License, Version 2.0.                       │
│  See the LICENSE file in the project root for more information.        │
└────────────────────────────────────────────────────────────────────────┘
*/

using com.RedPand.MCP.Plugin.Common;
using com.RedPand.MCP.Plugin.Common.Utils;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using NLog;

namespace com.RedPand.MCP.Plugin.Server.Transport
{
    public class StdioTransportLayer : ITransportLayer
    {
        public Consts.MCP.Server.TransportMethod TransportMethod
            => Consts.MCP.Server.TransportMethod.stdio;

        public IMcpServerBuilder ConfigureTransport(
            IMcpServerBuilder mcpServerBuilder,
            DataArguments dataArguments,
            Logger? logger = null)
        {
            return mcpServerBuilder.WithStdioServerTransport();
        }

        public void ConfigureServices(IServiceCollection services, DataArguments dataArguments)
        {
            services.AddHostedService<McpServerService>();
        }

        public void ConfigureApp(WebApplication app, DataArguments dataArguments)
        {
            // Stdio transport does not map any HTTP endpoints
        }
    }
}
