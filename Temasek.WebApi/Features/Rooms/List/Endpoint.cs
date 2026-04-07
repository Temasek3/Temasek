using FastEndpoints;
using Temasek.WebApi.Features.Rooms.Signboard;

namespace Temasek.WebApi.Features.Rooms.List;

public class Endpoint(RoomSignboardStore store) : EndpointWithoutRequest<IReadOnlyList<Response>>
{
    public override void Configure()
    {
        Get("/rooms");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var response = (await store.ListAsync(ct))
            .Select(room => new Response
            {
                RoomId = room.RoomId,
                Name = room.Name,
                ScheduleCount = room.ScheduleCount,
                UpdatedAtUtc = room.UpdatedAtUtc,
            })
            .ToArray();

        await Send.OkAsync(response, ct);
    }
}
