using FastEndpoints;
using Temasek.WebApi.Features.Rooms.Signboard.Contracts;

namespace Temasek.WebApi.Features.Rooms.Signboard.Get;

public class Endpoint(RoomSignboardStore store) : EndpointWithoutRequest<RoomSignboardResponse>
{
    public override void Configure()
    {
        Get("/rooms/{roomId}/signboard");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var roomId = Route<string>("roomId") ?? string.Empty;
        var response = await store.GetAsync(roomId, ct);

        await Send.OkAsync(response, ct);
    }
}
