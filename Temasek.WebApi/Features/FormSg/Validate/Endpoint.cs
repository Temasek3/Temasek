using System;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using FastEndpoints;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Temasek.WebApi.Features.FormSg.Validate;

public class Endpoint(IOptions<FormSgOptions> formSgOptions) : EndpointWithoutRequest<Response>
{
    public const string Issuer = "Temasek.WebApi.Features.FormSg.Validate";

    private readonly byte[] secretKeyBytes = Encoding.UTF8.GetBytes(
        formSgOptions.Value.SecretKey
            ?? throw new ArgumentNullException(nameof(formSgOptions.Value.SecretKey))
    );

    public override void Configure()
    {
        Get("formsg/validate");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Issuer = Issuer,
            Subject = User.Identities.First(),
            Expires = DateTime.UtcNow.AddMinutes(15),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(secretKeyBytes),
                SecurityAlgorithms.HmacSha256Signature
            ),
        };
        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);

        await Send.OkAsync(
            new Response
            {
                FormId =
                    formSgOptions.Value.FormId
                    ?? throw new ArgumentNullException(nameof(formSgOptions.Value.FormId)),
                PrefillFieldId =
                    formSgOptions.Value.PrefillFieldId
                    ?? throw new ArgumentNullException(nameof(formSgOptions.Value.PrefillFieldId)),
                ClerkUserId = tokenHandler.WriteToken(token),
            },
            ct
        );
    }
}
