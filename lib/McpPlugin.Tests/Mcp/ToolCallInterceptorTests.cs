// RedPand MCP. Licensed under the Apache License, Version 2.0.
#nullable enable
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using com.RedPand.MCP.Plugin.Common.Model;
using com.IvanMurzak.ReflectorNet;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace com.RedPand.MCP.Plugin.Tests.Mcp
{
    [Collection("McpPlugin")]
    public class ToolCallInterceptorTests
    {
        static readonly IReadOnlyDictionary<string, JsonElement> NoArgs = new Dictionary<string, JsonElement>();

        [Fact]
        public async Task AKeptAnswer_IsGivenWithoutRunningTheTool()
        {
            var tool = new CountingTool();
            var manager = ToolManager(tool);
            var interceptor = new Interceptor { Kept = ResponseCallTool.Success("the first run's result") };
            manager.Interceptor = interceptor;

            var response = await manager.RunCallTool(new RequestCallTool("request-1", CountingTool.ToolName, NoArgs));

            tool.Runs.ShouldBe(0);
            response.Status.ShouldBe(ResponseStatus.Success);
            response.RequestID.ShouldBe("request-1");
            response.Value!.GetMessage().ShouldBe("the first run's result");
            interceptor.After.ShouldBe(0);
        }

        [Fact]
        public async Task ANewCall_RunsAndIsSeenAfter()
        {
            var tool = new CountingTool();
            var manager = ToolManager(tool);
            var interceptor = new Interceptor();
            manager.Interceptor = interceptor;

            await manager.RunCallTool(new RequestCallTool("request-1", CountingTool.ToolName, NoArgs) { Caller = "agent#00ff" });

            tool.Runs.ShouldBe(1);
            interceptor.Before.ShouldBe(1);
            interceptor.After.ShouldBe(1);
            interceptor.LastCaller.ShouldBe("agent#00ff");
            interceptor.LastResult!.GetMessage().ShouldBe("ran");
        }

        [Fact]
        public async Task AnInterceptorThatThrows_DoesNotFailTheCall()
        {
            var tool = new CountingTool();
            var manager = ToolManager(tool);
            manager.Interceptor = new Interceptor { Throw = true };

            var response = await manager.RunCallTool(new RequestCallTool("request-1", CountingTool.ToolName, NoArgs));

            tool.Runs.ShouldBe(1);
            response.Status.ShouldBe(ResponseStatus.Success);
        }

        [Fact]
        public async Task ARefusingGate_IsAskedBeforeTheInterceptor()
        {
            var tool = new CountingTool();
            var manager = ToolManager(tool);
            var interceptor = new Interceptor();
            manager.Interceptor = interceptor;
            manager.RunGate = _ => "read-only mode is on";

            await manager.RunCallTool(new RequestCallTool("request-1", CountingTool.ToolName, NoArgs));

            interceptor.Before.ShouldBe(0);
            tool.Runs.ShouldBe(0);
        }

        static McpToolManager ToolManager(IRunTool tool)
        {
            var tools = new ToolRunnerCollection(new Reflector(), NullLogger.Instance);
            tools[tool.Name] = tool;
            return new McpToolManager(NullLogger<McpToolManager>.Instance, new Reflector(), tools);
        }

        sealed class Interceptor : IToolCallInterceptor
        {
            public ResponseCallTool? Kept;
            public bool Throw;
            public int Before;
            public int After;
            public string? LastCaller;
            public ResponseCallTool? LastResult;

            ResponseCallTool? IToolCallInterceptor.Before(RequestCallTool request, IRunTool tool)
            {
                if (Throw) throw new InvalidOperationException("broken interceptor");
                Before++;
                LastCaller = request.Caller;
                return Kept;
            }

            void IToolCallInterceptor.After(RequestCallTool request, IRunTool tool, ResponseCallTool result)
            {
                if (Throw) throw new InvalidOperationException("broken interceptor");
                After++;
                LastResult = result;
            }
        }

        sealed class CountingTool : IRunTool
        {
            public const string ToolName = "counting";
            public int Runs;

            public string Name => ToolName;
            public string? Title => "Counting";
            public string? Description => "Counts its runs";
            public string? SkillDescription => null;
            public string? SkillBody => null;
            public JsonNode? InputSchema => JsonNode.Parse("{}");
            public JsonNode? OutputSchema => null;
            public bool Visible { get; set; } = true;
            public bool? ReadOnlyHint => false;
            public bool? DestructiveHint => null;
            public bool? IdempotentHint => null;
            public bool? OpenWorldHint => null;
            public int TokenCount => 0;

            public Task<ResponseCallTool> Run(string requestId, IReadOnlyDictionary<string, JsonElement>? namedParameters, CancellationToken cancellationToken = default)
            {
                Runs++;
                return Task.FromResult(ResponseCallTool.Success("ran").SetRequestID(requestId));
            }
        }
    }
}
