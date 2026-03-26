using System;
using FastEndpoints;
using Temasek.WebApi.Entities;

namespace Temasek.WebApi.Features.Pokedex.List;

public class Endpoint(IFreeSql sql) : EndpointWithoutRequest
{
    public override void Configure()
    {
        Get("/pokedex");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var pokedex = await sql.Select<User>().ToListAsync(ct);
    }
}
