using FastEndpoints;
using Temasek.WebApi.Entities;

namespace Temasek.WebApi.Features.Rooms.Schedule.Update;

public class Endpoint(IFreeSql sql, RoomScheduleEventBus eventBus) : Endpoint<Request, Response>
{
    public override void Configure()
    {
        Put("/rooms/{RoomId}/schedule");
        AllowAnonymous();
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var roomId = RoomId.From(Route<string>("RoomId"));
        var room = await sql.Select<Room>().Where(item => item.RoomId == roomId).FirstAsync(ct);
        var exists = room is not null;
        var schedule = RequestMapper.ToSchedule(req);
        var requestedName = RoomName.From(req.Name);

        room ??= new Room { RoomId = roomId };
        room.Name =
            !requestedName.IsEmpty ? requestedName
            : !room.Name.IsEmpty ? room.Name
            : RoomName.From(room.RoomId.Value);
        room.Schedule = schedule;
        room.UpdatedAtUtc = DateTime.UtcNow;

        if (exists)
        {
            await sql.Update<Room>().SetSource(room).ExecuteAffrowsAsync(ct);
        }
        else
        {
            await sql.Insert(room).ExecuteAffrowsAsync(ct);
        }

        eventBus.Publish(room);

        await Send.OkAsync(ResponseMapper.ToResponse(room), ct);
    }
}
