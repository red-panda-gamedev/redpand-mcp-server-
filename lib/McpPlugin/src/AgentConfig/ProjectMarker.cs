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
using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace com.RedPand.MCP.Plugin.AgentConfig
{
    /// <summary>
    /// The tool-neutral, committable per-project marker file
    /// <c>&lt;project&gt;/.redpand-mcp/project.json</c>.
    ///
    /// <para>
    /// It records the user's optional explicit <see cref="PortOverride"/>. <see cref="ProjectIdentity"/>
    /// resolution and every config writer (engine UI, CLIs, <c>configure</c>) consult it, so an override can
    /// never silently diverge between the plugin and a terminal-written config. A marker written before the
    /// rename, in <see cref="LegacyDirectoryName"/>, is still read; the next write goes to the new place.
    /// </para>
    ///
    /// <para><b>This file is non-secret and safe to commit.</b> Credentials are never written here.</para>
    /// </summary>
    public sealed class ProjectMarker
    {
        /// <summary>Directory name (under the project root) that holds the marker.</summary>
        public const string DirectoryName = ".redpand-mcp";

        /// <summary>Where the marker lived before RedPand: read when the new one is missing.</summary>
        public const string LegacyDirectoryName = ".ai-game-dev";

        /// <summary>Marker file name inside <see cref="DirectoryName"/>.</summary>
        public const string FileName = "project.json";

        private static readonly JsonSerializerOptions SerializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };

        /// <summary>The user's explicit local-port override. When set it wins over the derived port. Optional.</summary>
        [JsonPropertyName("portOverride")]
        public int? PortOverride { get; set; }

        /// <summary>Absolute path of the marker file for a given project root.</summary>
        public static string PathFor(string projectRoot)
        {
            if (projectRoot == null)
                throw new ArgumentNullException(nameof(projectRoot));
            return Path.Combine(projectRoot, DirectoryName, FileName);
        }

        /// <summary>
        /// Read the marker for <paramref name="projectRoot"/>. Returns <c>null</c> when the marker file
        /// does not exist (a project that has never been enrolled / configured).
        /// </summary>
        public static ProjectMarker? Read(string projectRoot)
        {
            var path = PathFor(projectRoot);
            if (!File.Exists(path))
                path = Path.Combine(projectRoot, LegacyDirectoryName, FileName);
            if (!File.Exists(path))
                return null;

            var json = File.ReadAllText(path);
            if (string.IsNullOrWhiteSpace(json))
                return new ProjectMarker();

            return JsonSerializer.Deserialize<ProjectMarker>(json, SerializerOptions) ?? new ProjectMarker();
        }

        /// <summary>
        /// Write this marker into <paramref name="projectRoot"/>, creating the
        /// <c>.redpand-mcp</c> directory if needed. Non-secret; standard file permissions.
        /// </summary>
        public void Write(string projectRoot)
        {
            var path = PathFor(projectRoot);
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            var json = JsonSerializer.Serialize(this, SerializerOptions);
            File.WriteAllText(path, json);
        }
    }
}
