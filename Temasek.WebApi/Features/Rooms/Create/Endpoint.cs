using FastEndpoints;
using Temasek.WebApi.Entities;
using Temasek.WebApi.Features.Rooms.Schedule;

namespace Temasek.WebApi.Features.Rooms.Create;

public class Endpoint(IFreeSql sql) : Endpoint<Request, Response>
{
    public override void Configure()
    {
        Post("/rooms");
        AllowAnonymous();
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var normalizedRoomId = RoomId.From(req.RoomId);
        if (normalizedRoomId.IsEmpty)
        {
            AddError(r => r.RoomId, "Room ID is required.");
            await Send.ErrorsAsync(cancellation: ct);
            return;
        }
        var existing = await sql.Select<Room>()
            .Where(item => item.RoomId == normalizedRoomId)
            .FirstAsync(ct);

        if (existing is not null)
        {
            AddError(r => r.RoomId, "A room with this Room ID already exists.");
            await Send.ErrorsAsync(409, ct);
            return;
        }

        var requestedName = RoomName.From(req.Name);
        var roomEntity = new Room
        {
            RoomId = normalizedRoomId,
            Name = !requestedName.IsEmpty ? requestedName : RoomName.From(normalizedRoomId.Value),
            Schedule = RoomSchedule.Empty,
            UpdatedAtUtc = DateTime.UtcNow,
        };
        await sql.Insert(roomEntity).ExecuteAffrowsAsync(ct);

        await Send.OkAsync(
            new Response
            {
                RoomId = roomEntity.RoomId.Value,
                Name = roomEntity.Name.Value,
                ScheduleCount = roomEntity.Schedule.Count,
                UpdatedAtUtc = roomEntity.UpdatedAtUtc,
            },
            ct
        );
    }
}
