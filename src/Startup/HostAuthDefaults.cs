/*
┌───────────────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)                      │
│  Repository: GitHub (https://github.com/IvanMurzak/GameDev-MCP-Server)    │
│  Copyright (c) 2026 Ivan Murzak                                           │
│  Licensed under the Apache License, Version 2.0.                          │
│  See the LICENSE file in the project root for more information.           │
└───────────────────────────────────────────────────────────────────────────┘
*/

using System;
using com.RedPand.MCP.Plugin.Common;
using com.RedPand.MCP.Plugin.Common.Utils;

namespace com.RedPand.MCP.Server.Startup
{
    /// <summary>
    /// Host-level policy for the authentication default. <see cref="DataArguments"/> defaults auth to
    /// <see cref="Consts.MCP.Server.AuthOption.none"/> for both transports; an http server started without an
    /// explicit <c>--auth</c> stays on <c>none</c> and the host warns the operator. RedPand: the hosted
    /// cloud's <c>oauth</c> default for http went with the cloud. An explicit <c>--auth</c>/<c>--authorization</c>
    /// (or <c>MCP_AUTH</c>/<c>MCP_AUTHORIZATION</c>) always wins and is left untouched.
    /// </summary>
    public static class HostAuthDefaults
    {
        public enum Decision
        {
            /// <summary>Explicit auth was given, or transport is stdio — leave DataArguments' value as-is.</summary>
            LeaveAsConfigured,

            /// <summary>Http without an explicit auth — stay on <c>none</c> and warn the operator.</summary>
            DefaultHttpToNoneWithWarning
        }

        /// <summary>
        /// True when the operator explicitly chose an auth mode via env (<c>MCP_AUTH</c> /
        /// <c>MCP_AUTHORIZATION</c>) or CLI (<c>--auth</c> / <c>--authorization</c>). The library
        /// arg parser (<see cref="ArgsUtils.ParseLineArguments"/>) is reused so key detection matches
        /// exactly how <see cref="DataArguments"/> itself reads the args.
        /// </summary>
        public static bool IsAuthExplicitlySet(string[] args)
        {
            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable(Consts.MCP.Server.Env.Auth)))
                return true;
            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable(Consts.MCP.Server.Env.Authorization)))
                return true;

            var parsed = ArgsUtils.ParseLineArguments(args ?? Array.Empty<string>());
            return parsed.ContainsKey(Consts.MCP.Server.Args.Auth)
                || parsed.ContainsKey(Consts.MCP.Server.Args.Authorization);
        }

        /// <summary>Decide the host's auth default for the resolved transport. Pure — no side effects.</summary>
        public static Decision Decide(Consts.MCP.Server.TransportMethod transport, bool authExplicitlySet)
        {
            if (authExplicitlySet || transport != Consts.MCP.Server.TransportMethod.streamableHttp)
                return Decision.LeaveAsConfigured;
            return Decision.DefaultHttpToNoneWithWarning;
        }
    }
}
