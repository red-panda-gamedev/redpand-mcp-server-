// RedPand MCP. Licensed under the Apache License, Version 2.0.
using com.RedPand.MCP.Plugin.Common.Model;

namespace com.RedPand.MCP.Plugin
{
    /// <summary>
    /// Sees every tool call around its run (McpPlugin 8.20.0+). The engine plugin uses it to answer a call the server sent
    /// again after a reload with the result the first delivery already produced, so no call runs twice; and to keep a
    /// record of who did what in a shared editor.
    /// </summary>
    public interface IToolCallInterceptor
    {
        /// <summary>
        /// Before the tool runs (after <see cref="IToolManager.RunGate"/>): an answer to give instead of running it, such
        /// as the kept result of this same request, or null to run it. Must not throw.
        /// </summary>
        ResponseCallTool? Before(RequestCallTool request, IRunTool tool);

        /// <summary>After the tool answered, a "processing" answer of a deferred tool included. Must not throw.</summary>
        void After(RequestCallTool request, IRunTool tool, ResponseCallTool result);
    }
}
