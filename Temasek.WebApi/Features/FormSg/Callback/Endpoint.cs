using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Clerk.BackendAPI;
using Clerk.BackendAPI.Models.Operations;
using FastEndpoints;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Temasek.WebApi.Entities;

namespace Temasek.WebApi.Features.FormSg.Callback;

public class Endpoint(
    ILogger<Endpoint> logger,
    IOptions<FormSgOptions> formSgOptions,
    ClerkBackendApi clerk,
    IFreeSql sql
) : Endpoint<Request>
{
    private readonly byte[] secretKeyBytes = Encoding.UTF8.GetBytes(
        formSgOptions.Value.SecretKey
            ?? throw new ArgumentNullException(nameof(formSgOptions.Value.SecretKey))
    );

    public override void Configure()
    {
        Post("formsg/callback");
        AllowAnonymous();
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        if (HttpContext.Request.Headers["X-API-KEY"] != formSgOptions.Value.CallbackApiKey)
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        var tokenHandler = new JwtSecurityTokenHandler();

        var principal = await tokenHandler.ValidateTokenAsync(
            req.ClerkUserId,
            new TokenValidationParameters
            {
                // I don't think this would cause any security issues since we only need to get the data
                ValidateAudience = false,
                ValidIssuer = Validate.Endpoint.Issuer,
                ValidateLifetime = true,
                IssuerSigningKey = new SymmetricSecurityKey(secretKeyBytes),
            }
        );

        if (principal.IsValid == false)
        {
            logger.LogWarning(
                "Failed to verify Clerk User ID token: {Message}",
                principal.Exception.Message
            );
            await Send.ForbiddenAsync(ct);
            return;
        }

        var userId = principal
            .ClaimsIdentity.Claims.SingleOrDefault(c => c.Type == ClaimTypes.NameIdentifier)
            ?.Value;
        if (userId is null)
        {
            logger.LogWarning("Clerk User ID token does not contain NameIdentifier claim");
            await Send.ForbiddenAsync(cancellation: ct);
            return;
        }

        _ = await clerk.Users.GetAsync(userId);

        var clerkAccount = await sql.Select<ClerkAccount>()
            .Where(account => account.ClerkId == userId)
            .FirstAsync(ct);

        var clerkAccountUser = clerkAccount is null
            ? null
            : await sql.Select<User>()
                .Where(existingUser => existingUser.UserId == clerkAccount.UserId)
                .FirstAsync(ct);

        var nricMatchedUser = await sql.Select<User>()
            .Where(existingUser => existingUser.Nric == req.Nric)
            .FirstAsync(ct);

        var localUser = nricMatchedUser ?? clerkAccountUser ?? new User();

        localUser.Nric = req.Nric;
        localUser.Name = req.Name;

        if (localUser.UserId == 0)
        {
            localUser.UserId = await sql.Insert(localUser).ExecuteIdentityAsync(ct);
        }
        else
        {
            await sql.Update<User>().SetSource(localUser).ExecuteAffrowsAsync(ct);
        }

        if (clerkAccount is null)
        {
            clerkAccount = new ClerkAccount { ClerkId = userId, UserId = localUser.UserId };

            clerkAccount.ClerkAccountId = await sql.Insert(clerkAccount).ExecuteIdentityAsync(ct);
        }
        else if (clerkAccount.UserId != localUser.UserId)
        {
            clerkAccount.UserId = localUser.UserId;
            await sql.Update<ClerkAccount>().SetSource(clerkAccount).ExecuteAffrowsAsync(ct);
        }

        await clerk.Users.UpdateMetadataAsync(
            userId,
            new UpdateUserMetadataRequestBody
            {
                PublicMetadata = new Dictionary<string, object>
                {
                    { "nric", req.Nric },
                    { "name", req.Name },
                },
            }
        );

        await Send.OkAsync(cancellation: ct);
    }
}
