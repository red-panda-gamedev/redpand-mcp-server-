/*
┌────────────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)                   │
│  Repository: GitHub (https://github.com/IvanMurzak/MCP-Plugin-dotnet)  │
│  Copyright (c) 2025 Ivan Murzak                                        │
│  Licensed under the Apache License, Version 2.0.                       │
│  See the LICENSE file in the project root for more information.        │
└────────────────────────────────────────────────────────────────────────┘
*/

#nullable enable
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using static com.RedPand.MCP.Plugin.Common.Consts.MCP.Server;

namespace com.RedPand.MCP.Plugin.AgentConfig.Impl
{
    /// <summary>Configurator for the Claude Code AI agent.</summary>
    public sealed class ClaudeCodeConfigurator : AiAgentConfigurator
    {
        public override string AgentName => "Claude Code";
        public override string AgentId => "claude-code";
        public override string DownloadUrl => "https://docs.anthropic.com/en/docs/claude-code/overview";
        public override string TutorialUrl => "https://youtu.be/Sknh2p12W8c";
        public override string? SkillsPath => ".claude/skills";
        public override string? IconName => "claude-64.png";

        private static string LocalConfigPath(AgentConfiguratorSettings s) => Path.Combine(s.ProjectRootPath, ".mcp.json");

        /// <summary>
        /// The bearer written when the server takes no token. Claude Code's <c>/mcp</c> offers "Authenticate" (an OAuth
        /// sign-in) on every HTTP server whose entry has no <c>Authorization</c> header, and this server has no OAuth; with
        /// a header Claude Code uses it as the credential and never falls back to OAuth. A server in the <c>none</c> mode
        /// ignores it; in the <c>token</c> mode the real token is written instead.
        /// </summary>
        public const string NoAuthBearer = "none";

        static string HttpBearer(AgentConfiguratorSettings settings, HttpCredentialMode credentialMode)
            => credentialMode == HttpCredentialMode.AccessToken && !string.IsNullOrEmpty(settings.HttpBearerToken)
                ? settings.HttpBearerToken!
                : NoAuthBearer;

        protected override void ApplyHttpAuthorization(AiAgentConfig config, AgentConfiguratorSettings settings, HttpCredentialMode credentialMode)
            => config.ApplyHttpAuthorization(isRequired: true, token: HttpBearer(settings, credentialMode));

        protected override AiAgentConfig CreateStdioConfig(AgentConfiguratorSettings settings, ILogger? logger)
            => new JsonAiAgentConfig(AgentName, LocalConfigPath(settings), bodyPath: "mcpServers", logger: logger)
                .SetProperty("command", JsonValue.Create(settings.ExecutableFullPath.Replace('\\', '/'))!, requiredForConfiguration: true, comparison: ValueComparisonMode.Path)
                .SetProperty("args", AgentConfigBuilders.StdioArgs(settings), requiredForConfiguration: true)
                .SetPropertyToRemove("type")
                .SetPropertyToRemove("url");

        protected override AiAgentConfig CreateHttpConfig(AgentConfiguratorSettings settings, ILogger? logger)
            => AgentConfigBuilders.JsonHttp(AgentName, LocalConfigPath(settings), settings, logger, bodyPath: "mcpServers");

        protected override IReadOnlyList<ConfigurationSection> BuildSections(
            AgentConfiguratorSettings settings, TransportMethod transport, ILogger? logger)
        {
            var token = !string.IsNullOrEmpty(settings.Token) ? settings.Token! : "<token>";

            if (transport == TransportMethod.stdio)
            {
                var authArgs = settings.IsStdioAuthRequired
                    ? $" {Args.Authorization}={AuthOption.required} {Args.Token}={token}"
                    : string.Empty;
                var addCommand = $"claude mcp add {AiAgentConfig.DefaultMcpServerName} \"{settings.ExecutableFullPath}\" port={settings.Port} plugin-timeout={settings.TimeoutMs} client-transport=stdio{authArgs}";
                return new[]
                {
                    new ConfigurationSection("Start", true, new[]
                    {
                        ConfigurationItem.Description("Navigate to project root"),
                        ConfigurationItem.ReadOnlyField($"cd \"{settings.ProjectRootPath}\""),
                        ConfigurationItem.Description("Launch Claude Code"),
                        ConfigurationItem.ReadOnlyField("claude")
                    }),
                    new ConfigurationSection("Manual Configuration Steps", false, new[]
                    {
                        ConfigurationItem.Description("Run the following command in the project folder to configure Claude Code"),
                        ConfigurationItem.ReadOnlyField(addCommand),
                        ConfigurationItem.Description("Restart or start Claude Code to apply the configuration"),
                        ConfigurationItem.ReadOnlyField("claude")
                    })
                };
            }

            // The manual command must match what Configure writes: the local token in token mode, else the
            // no-auth bearer that keeps Claude Code from offering an OAuth sign-in.
            var authHeader = $" --header \"Authorization: Bearer {HttpBearer(settings, settings.ResolveHttpCredentialMode())}\"";
            // B8: the displayed "manual" command must use the SAME pinned URL that Configure writes into
            // .mcp.json (settings.PinnedHttpUrl, i.e. .../mcp/p/<pin>), not the bare unpinned host — an
            // unpinned URL only routes when the account has a single instance.
            var addCommandHttp = $"claude mcp add --transport http {AiAgentConfig.DefaultMcpServerName} {settings.PinnedHttpUrl}{authHeader}";
            return new[]
            {
                new ConfigurationSection("Start", true, new[]
                {
                    ConfigurationItem.Description("Navigate to project root"),
                    ConfigurationItem.ReadOnlyField($"cd \"{settings.ProjectRootPath}\""),
                    ConfigurationItem.Description("Launch Claude Code"),
                    ConfigurationItem.ReadOnlyField("claude")
                }),
                new ConfigurationSection("Manual Configuration Steps", false, new[]
                {
                    ConfigurationItem.Description("Run the following command in the project folder to configure Claude Code"),
                    ConfigurationItem.ReadOnlyField(addCommandHttp),
                    ConfigurationItem.Description("Restart or start Claude Code to apply the configuration"),
                    ConfigurationItem.ReadOnlyField("claude")
                })
            };
        }

        protected override IReadOnlyList<ConfigurationSection> BuildTroubleshootingSections(
            AgentConfiguratorSettings settings, TransportMethod transport, ILogger? logger)
            => TroubleshootingSection(
                "- Ensure Claude Code CLI is installed and accessible from terminal",
                "- Ensure Claude Code CLI is started in the same folder where the project is located. This folder must contain the Assets folder inside",
                "- Ensure Claude Code is configured with the same port as the plugin right now",
                "- Check that the configuration file .mcp.json exists",
                "- Restart Claude Code after configuration changes");
    }
}
