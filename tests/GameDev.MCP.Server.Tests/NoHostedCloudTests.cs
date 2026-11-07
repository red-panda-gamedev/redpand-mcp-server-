// RedPand MCP. Licensed under the Apache License, Version 2.0.

#nullable enable
using System;
using System.IO;
using System.Linq;
using Xunit;

namespace com.RedPand.MCP.Server.Tests
{
    /// <summary>
    /// The hosted cloud (sign-in, OAuth, project keys, the Cloud mode) was removed from the server and the
    /// plugin library. Its domain must not come back into their sources.
    /// </summary>
    public class NoHostedCloudTests
    {
        static readonly string[] SourceFolders =
        {
            "src",
            "lib/McpPlugin/src",
            "lib/McpPlugin.Common/src",
            "lib/McpPlugin.Server/src",
        };

        static string RepoRoot()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null && !File.Exists(Path.Combine(dir.FullName, "com.RedPand.MCP.Server.csproj")))
                dir = dir.Parent;
            return dir?.FullName ?? throw new InvalidOperationException("repository root not found above " + AppContext.BaseDirectory);
        }

        [Fact]
        public void SourcesDoNotNameTheCloudDomain()
        {
            var root = RepoRoot();
            var hits = SourceFolders
                .SelectMany(folder => Directory.EnumerateFiles(Path.Combine(root, folder), "*", SearchOption.AllDirectories))
                .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                            && !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
                .Where(path => File.ReadAllText(path).Contains("ai-game.dev", StringComparison.OrdinalIgnoreCase))
                .Select(path => Path.GetRelativePath(root, path))
                .ToList();

            Assert.True(hits.Count == 0, "ai-game.dev in: " + string.Join(", ", hits));
        }

        [Fact]
        public void ServerJsonDoesNotNameTheCloudDomain()
        {
            var text = File.ReadAllText(Path.Combine(RepoRoot(), "server.json"));
            Assert.DoesNotContain("ai-game.dev", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("\"oauth\"", text, StringComparison.Ordinal);
        }
    }
}
