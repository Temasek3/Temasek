using FastEndpoints;

namespace Temasek.WebApi.Features.Index.Get;

public class Endpoint : EndpointWithoutRequest
{
    public override void Configure()
    {
        Get("/");
        AllowAnonymous();
    }

    public override Task HandleAsync(CancellationToken ct)
    {
        return Send.RedirectAsync("/api/scalar");
    }
}
