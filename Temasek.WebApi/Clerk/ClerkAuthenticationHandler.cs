using Clerk.BackendAPI.Helpers.Jwks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Text.Encodings.Web;

namespace Temasek.WebApi.Clerk;

public class ClerkAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    IOptionsMonitor<ClerkOptions> clerkOptions,
    ILoggerFactory logger,
    UrlEncoder encoder
) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "Clerk";

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var options = new AuthenticateRequestOptions(
            secretKey: clerkOptions.CurrentValue.SecretKey,
            authorizedParties: clerkOptions.CurrentValue.AuthorizedParties
        );

        var requestState = await AuthenticateRequest.AuthenticateRequestAsync(Request, options);

        if (!requestState.IsAuthenticated)
        {
            return AuthenticateResult.Fail("Unauthorized as user is not authenticated.");
        }

        if (requestState.Claims is null)
        {
            return AuthenticateResult.Fail("Unauthorized as claims are missing.");
        }

        var ticket = new AuthenticationTicket(requestState.Claims, Scheme.Name);

        return AuthenticateResult.Success(ticket);
    }
}