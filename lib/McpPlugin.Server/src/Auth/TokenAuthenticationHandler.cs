/*
┌────────────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)                   │
│  Repository: GitHub (https://github.com/IvanMurzak/MCP-Plugin-dotnet)  │
│  Copyright (c) 2025 Ivan Murzak                                        │
│  Licensed under the Apache License, Version 2.0.                       │
│  See the LICENSE file in the project root for more information.        │
└────────────────────────────────────────────────────────────────────────┘
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using com.RedPand.MCP.Plugin.Common;
using com.RedPand.MCP.Plugin.Server.Webhooks.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace com.RedPand.MCP.Plugin.Server.Auth
{
    public class TokenAuthenticationHandler : AuthenticationHandler<TokenAuthenticationOptions>
    {
        public const string SchemeName = "McpPluginToken";
        public const string TokenClaimType = "mcp_plugin_token";
        public const string SubjectClaimType = "sub";
        public const string ScopeClaimType = "scope";
        public const string ClientIdClaimType = "client_id";

        readonly IAuthorizationWebhookService _authorizationWebhookService;

        public TokenAuthenticationHandler(
            IOptionsMonitor<TokenAuthenticationOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            IAuthorizationWebhookService authorizationWebhookService)
            : base(options, logger, encoder)
        {
            _authorizationWebhookService = authorizationWebhookService ?? throw new ArgumentNullException(nameof(authorizationWebhookService));
        }

        protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
        {
            Response.StatusCode = 401;

            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            Response.Headers.WWWAuthenticate = $"Bearer realm=\"MCP Plugin Server\", resource=\"{baseUrl}\"";
            await Response.WriteAsJsonAsync(new
            {
                error = "Unauthorized",
                message = "A valid Bearer token is required. Provide it via the Authorization header: Bearer <token>"
            });
        }

        protected override async Task HandleForbiddenAsync(AuthenticationProperties properties)
        {
            Response.StatusCode = 403;
            await Response.WriteAsJsonAsync(new
            {
                error = "Forbidden",
                message = "You do not have permission to access this resource."
            });
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            // Two modes, chosen by the resolved connection strategy (RedPand: the OAuth resource-server mode
            // of the hosted cloud was removed):
            //   • LocalTokenMode (mcp-authorize g6) — offline shared-secret: constant-time compare the
            //     presented bearer against the configured --token secret.
            //   • neither → `none` (offline / local dev / CI): anonymous, no token accepted.
            // The legacy multi-tier shared-token / DCR token-equality paths were removed in b5; the g6
            // local-token path is a single constant-time comparison, not their reintroduction.
            if (Options.LocalTokenMode)
                return Task.FromResult(HandleLocalTokenAuthenticate());
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        // ── Offline local-token path (mcp-authorize g6) ───────────────────────────────────────────
        AuthenticateResult HandleLocalTokenAuthenticate()
        {
            if (!TryGetBearerToken(out var token))
            {
                // No/empty/non-Bearer credential. On the hub path stay silent (BaseHub does its own
                // auth via the strategy). Elsewhere NoResult lets the RequireAuthorization pipeline
                // issue the non-OAuth `Bearer realm="MCP Plugin Server"` 401 challenge.
                return AuthenticateResult.NoResult();
            }

            // Constant-time compare over SHA-256 digests — no length/content timing side channel, and a
            // deliberate upgrade over the deleted b5 `string.Equals(Ordinal)`.
            if (!TokenComparison.FixedTimeEquals(token, Options.LocalToken))
            {
                var isHubPath = IsHubPath();
                if (!isHubPath)
                {
                    Logger.LogWarning(
                        "MCP local-token auth rejected: token mismatch. RemoteIp: {RemoteIpAddress}, UserAgent: {UserAgent}, RequestPath: {RequestPath}, TokenFingerprint: {TokenFingerprint}",
                        GetClientIpAddress(), Request.Headers.UserAgent.ToString(), Request.Path.Value, FingerprintToken(token));
                }
                return isHubPath
                    ? AuthenticateResult.NoResult()
                    : AuthenticateResult.Fail("Invalid token.");
            }

            var claims = new List<Claim> { new Claim(TokenClaimType, token) };
            var identity = new ClaimsIdentity(claims, SchemeName);
            var principal = new ClaimsPrincipal(identity);
            return AuthenticateResult.Success(new AuthenticationTicket(principal, SchemeName));
        }

        bool TryGetBearerToken(out string token)
        {
            token = string.Empty;
            var authHeader = Request.Headers["Authorization"].ToString();
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                return false;
            token = authHeader.Substring("Bearer ".Length).Trim();
            return token.Length > 0;
        }

        // True when the request targets the SignalR hub endpoint. Used to silence the
        // "[7] McpPluginToken was not authenticated" info log noise on hub probes — see
        // the invalid-token branch in HandleLocalTokenAuthenticate (BaseHub does its own auth).
        bool IsHubPath()
        {
            var path = Request.Path.Value;
            return !string.IsNullOrEmpty(path)
                && path.StartsWith(Consts.Hub.RemoteApp, StringComparison.OrdinalIgnoreCase);
        }

        string? GetClientIpAddress()
        {
            var forwardedFor = Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(forwardedFor))
                return forwardedFor.Split(',')[0].Trim();

            var realIp = Request.Headers["X-Real-IP"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(realIp))
                return realIp.Trim();

            return Request.HttpContext.Connection.RemoteIpAddress?.ToString();
        }

        static string? FingerprintToken(string? token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return null;

            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token.Trim()));
            return Convert.ToHexString(bytes).Substring(0, 16).ToLowerInvariant();
        }
    }

    public class TokenAuthenticationOptions : AuthenticationSchemeOptions
    {
        /// <summary>
        /// When true, the handler runs the offline local-token path (mcp-authorize g6): the presented
        /// bearer is compared against <see cref="LocalToken"/> in constant time. Set by <c>LocalTokenMcpStrategy.ConfigureAuthentication</c>.
        /// </summary>
        public bool LocalTokenMode { get; set; }

        /// <summary>
        /// The static shared secret accepted in <see cref="LocalTokenMode"/> (from <c>--token</c> /
        /// <c>MCP_PLUGIN_TOKEN</c>). Never logged; compared with a fixed-length constant-time digest.
        /// </summary>
        public string? LocalToken { get; set; }
    }
}
