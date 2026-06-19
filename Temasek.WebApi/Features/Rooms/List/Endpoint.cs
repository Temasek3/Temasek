using FastEndpoints;
using Temasek.WebApi.Entities;
using Temasek.WebApi.Features.Rooms.Schedule;

namespace Temasek.WebApi.Features.Rooms.List;

public class Endpoint(IFreeSql sql) : EndpointWithoutRequest<IReadOnlyList<Response>>
{
    public override void Configure()
    {
        Get("/rooms");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var response = (await sql.Select<Room>().OrderBy(item => item.Name).ToListAsync(ct))
            .Select(room => new Response
            {
                RoomId = room.RoomId.Value,
                Name = room.DisplayName,
                ScheduleCount = room.Schedule.Count,
                UpdatedAtUtc = room.UpdatedAtUtc,
            })
            .ToArray();

        await Send.OkAsync(response, ct);
    }
}
