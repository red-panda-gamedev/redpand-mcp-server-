// RedPand MCP. Licensed under the Apache License, Version 2.0.

using System.Text.Json;
using com.RedPand.MCP.Plugin.Common.Model;
using com.RedPand.MCP.Plugin.Server.Strategy;
using Shouldly;
using Xunit;

namespace com.RedPand.MCP.Plugin.Server.Tests
{
    /// <summary>
    /// When the plugin says its tools changed, agents get a <c>tools/list_changed</c> only if the set differs: a changed
    /// argument or description must count, or an agent keeps calling a tool with the arguments it had before.
    /// </summary>
    public class PluginCatalogToolSetTests
    {
        static ResponseListTool Tool(string name, string schema = "{\"type\":\"object\"}", string description = "d") => new ResponseListTool
        {
            Name = name,
            Description = description,
            InputSchema = JsonDocument.Parse(schema).RootElement.Clone(),
        };

        [Fact]
        public void TheSameToolsInAnotherOrderAreTheSameSet() =>
            PluginCatalogCache.SameToolSet(new[] { Tool("a"), Tool("b") }, new[] { Tool("b"), Tool("a") }).ShouldBeTrue();

        [Fact]
        public void ANewArgumentIsAChange() =>
            PluginCatalogCache.SameToolSet(
                new[] { Tool("test", "{\"type\":\"object\",\"properties\":{\"testMode\":{\"type\":\"string\"}}}") },
                new[] { Tool("test", "{\"type\":\"object\",\"properties\":{\"testMode\":{\"type\":\"string\"},\"failed\":{\"type\":\"boolean\"}}}") })
                .ShouldBeFalse();

        [Fact]
        public void ANewDescriptionIsAChange() =>
            PluginCatalogCache.SameToolSet(new[] { Tool("query") }, new[] { Tool("query", description: "reads the editor") }).ShouldBeFalse();

        [Fact]
        public void AToolMoreOrLessIsAChange() =>
            PluginCatalogCache.SameToolSet(new[] { Tool("a") }, new[] { Tool("a"), Tool("b") }).ShouldBeFalse();
    }
}
