using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IntelliDocs.IntegrationTests.Auth;

internal sealed class TestAuthenticationHandler
    : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string AuthenticationScheme = "Test";

    public const string ReviewerObjectId =
        "11111111-2222-3333-4444-555555555555";

    public const string UnauthenticatedHeader =
        "X-Test-Unauthenticated";

    public TestAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult>
        HandleAuthenticateAsync()
    {
        if (Request.Headers.ContainsKey(
                UnauthenticatedHeader))
        {
            return Task.FromResult(
                AuthenticateResult.NoResult());
        }

        var claims =
            new[]
            {
                new Claim(
                    "oid",
                    ReviewerObjectId),
                new Claim(
                    ClaimTypes.Name,
                    "reviewer@example.com")
            };

        var identity =
            new ClaimsIdentity(
                claims,
                AuthenticationScheme);

        var principal =
            new ClaimsPrincipal(identity);

        var ticket =
            new AuthenticationTicket(
                principal,
                AuthenticationScheme);

        return Task.FromResult(
            AuthenticateResult.Success(ticket));
    }
}