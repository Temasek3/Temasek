using FastEndpoints;
using Temasek.WebApi.Features.Rooms.Signboard.Contracts;

namespace Temasek.WebApi.Features.Rooms.Signboard.Update;

public class Endpoint(RoomSignboardStore store, RoomSignboardEventBus eventBus)
    : Endpoint<Request, RoomSignboardResponse>
{
    public override void Configure()
    {
        Put("/rooms/{roomId}/signboard");
        AllowAnonymous();
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var roomId = Route<string>("roomId") ?? string.Empty;
        var response = await store.SaveAsync(roomId, req.Name, req.Schedule, ct);

        eventBus.Publish(response);

        await Send.OkAsync(response, ct);
    }
}
