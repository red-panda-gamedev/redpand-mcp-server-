/*
┌────────────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)                   │
│  Repository: GitHub (https://github.com/IvanMurzak/MCP-Plugin-dotnet)  │
│  Copyright (c) 2025 Ivan Murzak                                        │
│  Licensed under the Apache License, Version 2.0.                       │
│  See the LICENSE file in the project root for more information.        │
└────────────────────────────────────────────────────────────────────────┘
*/
using System.Threading.Tasks;
using Shouldly;
using Xunit;

namespace com.RedPand.MCP.Plugin.Tests.Network.Connection.Credentials
{
    public sealed class HostScopeTests
    {
        [Theory]
        [InlineData("http://localhost")]
        [InlineData("http://localhost:8080")]
        [InlineData("https://LOCALHOST:443/hub")]
        [InlineData("http://LocalHost:5000/")]
        [InlineData("http://127.0.0.1")]
        [InlineData("http://127.0.0.1:11111")]
        [InlineData("http://127.255.255.254:80")]
        [InlineData("http://127.1.2.3")]
        [InlineData("http://127.1")] // System.Uri canonicalizes to 127.0.0.1, which is what the client dials
        [InlineData("http://[::1]")]
        [InlineData("http://[::1]:8080")]
        [InlineData("http://[0:0:0:0:0:0:0:1]:8080")]
        [InlineData("http://[::ffff:127.0.0.1]:8080")]
        [InlineData("  http://localhost:8080  ")]
        public void IsLoopback_True(string url) => HostScope.IsLoopback(url).ShouldBeTrue();

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("localhost:8080")]
        [InlineData("not a url")]
        [InlineData("https://ai-game.dev")]
        [InlineData("http://evil.com")]
        [InlineData("http://localhost@evil.com")]
        [InlineData("http://localhost:8080@evil.com")]
        [InlineData("http://user:pass@localhost:8080")]
        [InlineData("http://127.0.0.1.evil.com")]
        [InlineData("http://localhost.evil.com")]
        [InlineData("http://evil.localhost")]
        [InlineData("http://localhost.")]
        [InlineData("http://128.0.0.1")]
        [InlineData("http://126.255.255.255")]
        [InlineData("http://0.0.0.0")]
        [InlineData("http://10.0.0.1")]
        [InlineData("http://192.168.1.10:8080")]
        [InlineData("http://[::2]")]
        [InlineData("http://[::]")]
        [InlineData("http://[::ffff:10.0.0.1]")]
        [InlineData("ftp://localhost")]
        [InlineData("file:///C:/localhost")]
        public void IsLoopback_False(string? url) => HostScope.IsLoopback(url).ShouldBeFalse();

    }
}
