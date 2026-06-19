using FastEndpoints;
using Temasek.WebApi.Entities;

namespace Temasek.WebApi.Features.Rooms.Schedule.Get;

public class Endpoint(IFreeSql sql) : EndpointWithoutRequest<Response>
{
    public override void Configure()
    {
        Get("/rooms/{RoomId}/schedule");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var roomId = RoomId.From(Route<string>("RoomId"));
        var room =
            await sql.Select<Room>().Where(item => item.RoomId == roomId).FirstAsync(ct)
            ?? new Room
            {
                RoomId = roomId,
                Name = RoomName.From(roomId.Value),
                Schedule = RoomSchedule.Empty,
                UpdatedAtUtc = DateTime.UtcNow,
            };
        var response = ResponseMapper.ToResponse(room);

        await Send.OkAsync(response, ct);
    }
}
