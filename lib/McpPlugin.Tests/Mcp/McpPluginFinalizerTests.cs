#nullable enable
using System.Reflection;
using Shouldly;
using Xunit;

namespace com.RedPand.MCP.Plugin.Tests.Mcp
{
    /// <summary>
    /// McpPlugin had a finalizer that ran Dispose. In Unity it ran during the Mono cleanup at exit, logged through the
    /// console from the finalizer thread and crashed the Editor (boehm-gc assertion <c>type &lt; HANDLE_TYPE_MAX</c>,
    /// exit code -536870911). The plugin owns no unmanaged handle, so it declares no finalizer.
    /// </summary>
    public class McpPluginFinalizerTests
    {
        [Fact]
        public void McpPlugin_DeclaresNoFinalizer()
        {
            var finalizer = typeof(McpPlugin).GetMethod("Finalize",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            finalizer.ShouldBeNull("Dispose from the finalizer thread crashes Unity at exit");
        }
    }
}
