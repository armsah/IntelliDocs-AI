using System.Net.Http.Headers;
using Microsoft.Identity.Web;

namespace IntelliDocs.ReviewPortal.Services;

public sealed class ReviewApiAuthorizationHandler
    : DelegatingHandler
{
    private readonly ITokenAcquisition _tokenAcquisition;
    private readonly string[] _scopes;

    public ReviewApiAuthorizationHandler(
        ITokenAcquisition tokenAcquisition,
        IConfiguration configuration)
    {
        _tokenAcquisition = tokenAcquisition;

        _scopes =
            configuration
                .GetSection("ReviewApi:Scopes")
                .Get<string[]>()
            ?? [];
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (_scopes.Length == 0)
        {
            throw new InvalidOperationException(
                "ReviewApi:Scopes configuration is required.");
        }

        var accessToken =
            await _tokenAcquisition.GetAccessTokenForUserAsync(
                _scopes);

        request.Headers.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                accessToken);

        return await base.SendAsync(
            request,
            cancellationToken);
    }
}