/*
┌────────────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)                   │
│  Repository: GitHub (https://github.com/IvanMurzak/MCP-Plugin-dotnet)  │
│  Copyright (c) 2025 Ivan Murzak                                        │
│  Licensed under the Apache License, Version 2.0.                       │
│  See the LICENSE file in the project root for more information.        │
└────────────────────────────────────────────────────────────────────────┘
*/

using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using com.RedPand.MCP.Plugin.Common.Model;
using com.RedPand.MCP.Plugin.Common.Utils;
using com.RedPand.MCP.Plugin.Server.Tools;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using NLog;

namespace com.RedPand.MCP.Plugin.Server
{
    public static partial class ToolRouter
    {
        public static async ValueTask<CallToolResult> Call(RequestContext<CallToolRequestParams> request, CancellationToken cancellationToken)
        {
            var logger = LogManager.GetCurrentClassLogger();
            logger.Trace("{0}.Call", nameof(ToolRouter));

            if (request == null)
                return new CallToolResult().SetError("[Error] Request is null");

            if (request.Params == null)
                return new CallToolResult().SetError("[Error] Request.Params is null");

            if (request.Params.Arguments == null)
                return new CallToolResult().SetError("[Error] Request.Params.Arguments is null");

            if (request.Services == null)
                return new CallToolResult().SetError("[Error] Request.Services is null");

            var argumentsDict = request.Params.Arguments as IReadOnlyDictionary<string, JsonElement>
                ?? new Dictionary<string, JsonElement>();

            var requestData = new RequestCallTool(request.Params.Name, argumentsDict)
            {
                Caller = CallerLabel(request.Server?.ClientInfo?.Name, request.Server?.SessionId)
            };
            if (logger.IsTraceEnabled)
                logger.Trace("Call remote tool '{0}':\n{1}", request.Params.Name, requestData.ToPrettyJson());

            var response = await ToolDispatcher.CallAsync(request.Services, requestData, cancellationToken);
            if (response == null)
                return new CallToolResult().SetError($"[Error] '{nameof(response)}' is null");

            if (logger.IsTraceEnabled)
                logger.Trace("Call tool response:\n{0}", response.ToPrettyJson());

            if (response.Status == ResponseStatus.Error)
                return new CallToolResult().SetError(response.Message ?? "[Error] Got an error during running tool");

            if (response.Value == null)
                return new CallToolResult().SetError("[Error] Tool returned null value");

            return response.Value.ToCallToolResult();
        }

        /// <summary>
        /// A short, stable name for one agent session, e.g. "claude-code#3fa2": the client's name and four hex digits of a
        /// hash of the MCP session id (the id itself is not sent). Null when the session is unknown.
        /// </summary>
        public static string? CallerLabel(string? clientName, string? sessionId)
        {
            if (string.IsNullOrEmpty(clientName) && string.IsNullOrEmpty(sessionId))
                return null;
            var name = string.IsNullOrWhiteSpace(clientName) ? "agent" : clientName!.Trim();
            if (name.Length > 24)
                name = name.Substring(0, 24);
            if (string.IsNullOrEmpty(sessionId))
                return name;
            using var sha = System.Security.Cryptography.SHA256.Create();
            var hash = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(sessionId!));
            return name + "#" + hash[0].ToString("x2") + hash[1].ToString("x2");
        }
    }
}
