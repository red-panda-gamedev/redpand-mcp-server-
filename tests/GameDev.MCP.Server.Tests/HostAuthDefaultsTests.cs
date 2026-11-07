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
using com.RedPand.MCP.Server.Startup;
using com.RedPand.MCP.Plugin.Common;
using Xunit;

namespace com.RedPand.MCP.Server.Tests
{
    /// <summary>
    /// Covers the auth default policy: stdio and an explicit choice are left untouched; http without an
    /// explicit auth stays on none with a warning (the hosted cloud's oauth default was removed).
    /// </summary>
    public class HostAuthDefaultsTests
    {
        [Fact]
        public void Stdio_NoExplicitAuth_LeavesAsConfigured()
        {
            var decision = HostAuthDefaults.Decide(Consts.MCP.Server.TransportMethod.stdio, authExplicitlySet: false);
            Assert.Equal(HostAuthDefaults.Decision.LeaveAsConfigured, decision);
        }

        [Fact]
        public void Http_NoExplicitAuth_FallsBackToNoneWithWarning()
        {
            var decision = HostAuthDefaults.Decide(Consts.MCP.Server.TransportMethod.streamableHttp, authExplicitlySet: false);
            Assert.Equal(HostAuthDefaults.Decision.DefaultHttpToNoneWithWarning, decision);
        }

        [Fact]
        public void Http_ExplicitAuth_LeavesAsConfigured()
        {
            var decision = HostAuthDefaults.Decide(Consts.MCP.Server.TransportMethod.streamableHttp, authExplicitlySet: true);
            Assert.Equal(HostAuthDefaults.Decision.LeaveAsConfigured, decision);
        }

        [Fact]
        public void IsAuthExplicitlySet_AuthArg_True()
        {
            Assert.True(HostAuthDefaults.IsAuthExplicitlySet(new[] { "--auth", "oauth" }));
            Assert.True(HostAuthDefaults.IsAuthExplicitlySet(new[] { "--auth=none" }));
            Assert.True(HostAuthDefaults.IsAuthExplicitlySet(new[] { "--authorization", "none" }));
        }

        [Fact]
        public void IsAuthExplicitlySet_NoAuthSignal_False()
        {
            // Guard: no MCP_AUTH / MCP_AUTHORIZATION env leaking in from the test host.
            var savedAuth = Environment.GetEnvironmentVariable(Consts.MCP.Server.Env.Auth);
            var savedAuthorization = Environment.GetEnvironmentVariable(Consts.MCP.Server.Env.Authorization);
            try
            {
                Environment.SetEnvironmentVariable(Consts.MCP.Server.Env.Auth, null);
                Environment.SetEnvironmentVariable(Consts.MCP.Server.Env.Authorization, null);

                Assert.False(HostAuthDefaults.IsAuthExplicitlySet(new[] { "--client-transport", "streamableHttp", "--port", "8080" }));
                Assert.False(HostAuthDefaults.IsAuthExplicitlySet(Array.Empty<string>()));
            }
            finally
            {
                Environment.SetEnvironmentVariable(Consts.MCP.Server.Env.Auth, savedAuth);
                Environment.SetEnvironmentVariable(Consts.MCP.Server.Env.Authorization, savedAuthorization);
            }
        }

        [Fact]
        public void IsAuthExplicitlySet_AuthEnv_True()
        {
            var saved = Environment.GetEnvironmentVariable(Consts.MCP.Server.Env.Auth);
            try
            {
                Environment.SetEnvironmentVariable(Consts.MCP.Server.Env.Auth, "oauth");
                Assert.True(HostAuthDefaults.IsAuthExplicitlySet(Array.Empty<string>()));
            }
            finally
            {
                Environment.SetEnvironmentVariable(Consts.MCP.Server.Env.Auth, saved);
            }
        }
    }
}
