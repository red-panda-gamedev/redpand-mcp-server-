// RedPand MCP. Licensed under the Apache License, Version 2.0.

#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using com.RedPand.MCP.Plugin.Common.Model;
using com.RedPand.MCP.Plugin.Tests.Infrastructure;
using com.IvanMurzak.ReflectorNet;
using Microsoft.Extensions.Logging;
using Shouldly;
using Xunit;

namespace com.RedPand.MCP.Plugin.Tests.Mcp
{
    /// <summary>
    /// A red line in the console means the plugin is broken. A call that was answered (a wrong request, a busy editor)
    /// is one warning line without a stack, logged once.
    /// </summary>
    [Collection("McpPlugin")]
    public class ConsoleLogLevelTests
    {
        static readonly IReadOnlyDictionary<string, JsonElement> NoArgs = new Dictionary<string, JsonElement>();

        public static class Tools
        {
            public static ResponseCallTool AnswersAnError() => ResponseCallTool.Error("err 1:56 expected '|' or ';', got ','");
            public static string RejectsTheRequest() => throw new InvalidOperationException("no object 'Player'");
            public static string IsBusy() => throw new ToolUnavailableException("scripts are compiling", notExecuted: true);
            public static string IsBroken() => throw new NullReferenceException("a bug");
        }

        static async Task<(ResponseData<ResponseCallTool> Response, CapturingLogger Log)> Call(string method)
        {
            var log = new CapturingLogger();
            var reflector = new Reflector();
            var tools = new ToolRunnerCollection(reflector, log);
            tools["probe"] = RunTool.CreateFromStaticMethod(reflector, log, "probe", typeof(Tools).GetMethod(method)!, title: "Probe");
            var manager = new McpToolManager(new Typed<McpToolManager>(log), reflector, tools);
            var response = await manager.RunCallTool(new RequestCallTool("request-1", "probe", NoArgs));
            return (response, log);
        }

        static void AssertOneWarningLine(CapturingLogger log)
        {
            log.Entries.ShouldNotContain(e => e.Level >= LogLevel.Error);
            var warnings = log.Entries.Where(e => e.Level == LogLevel.Warning).ToList();
            warnings.Count.ShouldBe(1);
            warnings[0].Exception.ShouldBeNull();
        }

        [Theory]
        [InlineData(nameof(Tools.AnswersAnError))]
        [InlineData(nameof(Tools.RejectsTheRequest))]
        [InlineData(nameof(Tools.IsBusy))]
        public async Task AnAnsweredFailure_IsOneWarningWithoutAStack(string method)
        {
            var (response, log) = await Call(method);

            response.Status.ShouldBe(ResponseStatus.Error);
            AssertOneWarningLine(log);
        }

        [Fact]
        public async Task AFaultInThePlugin_IsAnErrorWithItsStack()
        {
            var (response, log) = await Call(nameof(Tools.IsBroken));

            response.ErrorKind.ShouldBe(ResponseErrorKind.Internal);
            log.Entries.ShouldContain(e => e.Level == LogLevel.Error
                && (e.Exception is NullReferenceException || e.Exception!.InnerException is NullReferenceException));
            log.Entries.ShouldNotContain(e => e.Level == LogLevel.Warning);
        }

        [Fact]
        public async Task AMissingTool_IsAWarning()
        {
            var log = new CapturingLogger();
            var reflector = new Reflector();
            var manager = new McpToolManager(new Typed<McpToolManager>(log), reflector, new ToolRunnerCollection(reflector, log));

            await manager.RunCallTool(new RequestCallTool("request-1", "unity-skill-generate", NoArgs));

            AssertOneWarningLine(log);
        }

        [Theory]
        [InlineData(ResponseErrorKind.None, LogLevel.Warning)]
        [InlineData(ResponseErrorKind.BadRequest, LogLevel.Warning)]
        [InlineData(ResponseErrorKind.NotFound, LogLevel.Warning)]
        [InlineData(ResponseErrorKind.Conflict, LogLevel.Warning)]
        [InlineData(ResponseErrorKind.Timeout, LogLevel.Warning)]
        [InlineData(ResponseErrorKind.Unavailable, LogLevel.Warning)]
        [InlineData(ResponseErrorKind.Internal, LogLevel.Error)]
        public void TheLevelFollowsTheKind(ResponseErrorKind kind, LogLevel level)
        {
            var log = new CapturingLogger();
            var ex = new Exception("the connection dropped");

            ResponseData<ResponseCallTool>.Error("request-1", "failed", kind).Log(log, ex: ex);

            var entry = log.Entries.Single();
            entry.Level.ShouldBe(level);
            entry.Exception.ShouldBe(level == LogLevel.Error ? ex : null);
        }

        sealed class Typed<T> : ILogger<T>
        {
            readonly ILogger _inner;
            public Typed(ILogger inner) => _inner = inner;
            public IDisposable BeginScope<TState>(TState state) where TState : notnull => _inner.BeginScope(state)!;
            public bool IsEnabled(LogLevel logLevel) => _inner.IsEnabled(logLevel);
            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
                => _inner.Log(logLevel, eventId, state, exception, formatter);
        }
    }
}
