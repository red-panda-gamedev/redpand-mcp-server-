// RedPand MCP. Licensed under the Apache License, Version 2.0.

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
    public class ToolRunGateTests
    {
        static readonly IReadOnlyDictionary<string, JsonElement> NoArgs = new Dictionary<string, JsonElement>();

        [Fact]
        public async Task ARefusedToolDoesNotRun_AndTheCallerReadsWhy()
        {
            var tool = new CountingTool();
            var manager = ToolManager(tool);
            manager.RunGate = t => t.Name == CountingTool.ToolName ? "read-only mode is on" : null;

            var response = await manager.RunCallTool(new RequestCallTool("request-1", CountingTool.ToolName, NoArgs));

            tool.Runs.ShouldBe(0);
            response.Status.ShouldBe(ResponseStatus.Error);
            response.RequestID.ShouldBe("request-1");
            response.Value.ShouldNotBeNull();
            response.Value!.Status.ShouldBe(ResponseStatus.Error);
            response.Value.GetMessage().ShouldBe("read-only mode is on");
            response.Value.HttpStatusCode.ShouldBe(403);
        }

        [Fact]
        public async Task AGateThatAnswersNull_LetsTheToolRun()
        {
            var tool = new CountingTool();
            var manager = ToolManager(tool);
            IRunTool? asked = null;
            manager.RunGate = t => { asked = t; return null; };

            var response = await manager.RunCallTool(new RequestCallTool("request-1", CountingTool.ToolName, NoArgs));

            tool.Runs.ShouldBe(1);
            asked.ShouldBeSameAs(tool);
            response.Status.ShouldBe(ResponseStatus.Success);
        }

        [Fact]
        public async Task WithoutAGate_ToolsRun()
        {
            var tool = new CountingTool();
            var manager = ToolManager(tool);

            await manager.RunCallTool(new RequestCallTool("request-1", CountingTool.ToolName, NoArgs));

            manager.RunGate.ShouldBeNull();
            tool.Runs.ShouldBe(1);
        }

        [Fact]
        public void TheGateIsReachableThroughTheInterface()
        {
            IToolManager manager = ToolManager(new CountingTool());
            System.Func<IRunTool, string?> gate = _ => null;

            manager.RunGate = gate;

            manager.RunGate.ShouldBeSameAs(gate);
        }

        static McpToolManager ToolManager(IRunTool tool)
        {
            var tools = new ToolRunnerCollection(new Reflector(), NullLogger.Instance);
            tools[tool.Name] = tool;
            return new McpToolManager(NullLogger<McpToolManager>.Instance, new Reflector(), tools);
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
