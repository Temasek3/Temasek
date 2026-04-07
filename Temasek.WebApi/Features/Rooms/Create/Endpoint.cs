using FastEndpoints;
using Temasek.WebApi.Features.Rooms.Signboard;

namespace Temasek.WebApi.Features.Rooms.Create;

public class Endpoint(RoomSignboardStore store) : Endpoint<Request, Response>
{
    public override void Configure()
    {
        Post("/rooms");
        AllowAnonymous();
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var roomId = req.RoomId?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(roomId))
        {
            AddError(r => r.RoomId, "Room ID is required.");
            await Send.ErrorsAsync(cancellation: ct);
            return;
        }

        var (room, created) = await store.CreateAsync(roomId, req.Name, ct);
        if (!created)
        {
            AddError(r => r.RoomId, "A room with this Room ID already exists.");
            await Send.ErrorsAsync(409, ct);
            return;
        }

        await Send.OkAsync(
            new Response
            {
                RoomId = room.RoomId,
                Name = room.Name,
                ScheduleCount = room.Schedule.Count,
                UpdatedAtUtc = room.UpdatedAtUtc,
            },
            ct
        );
    }
}
