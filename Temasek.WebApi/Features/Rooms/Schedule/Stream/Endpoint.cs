using System.Runtime.CompilerServices;
using FastEndpoints;
using Temasek.WebApi.Entities;

namespace Temasek.WebApi.Features.Rooms.Schedule.Stream;

public class Endpoint(IFreeSql sql, RoomScheduleEventBus eventBus) : EndpointWithoutRequest
{
    private const string ScheduleEventName = "schedule";

    public override void Configure()
    {
        Get("/rooms/{RoomId}/schedule/stream");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var roomId = RoomId.From(Route<string>("RoomId"));

        HttpContext.Response.Headers["Cache-Control"] = "no-cache";
        HttpContext.Response.Headers.Append("X-Accel-Buffering", "no");

        await Send.EventStreamAsync(ScheduleEventName, GetScheduleStream(roomId, ct), ct);
    }

    private async IAsyncEnumerable<Response> GetScheduleStream(
        RoomId roomId,
        [EnumeratorCancellation] CancellationToken ct
    )
    {
        var room =
            await sql.Select<Room>().Where(item => item.RoomId == roomId).FirstAsync(ct)
            ?? new Room
            {
                RoomId = roomId,
                Name = RoomName.From(roomId.Value),
                Schedule = RoomSchedule.Empty,
                UpdatedAtUtc = DateTime.UtcNow,
            };
        yield return ResponseMapper.ToResponse(room);

        var subscription = eventBus.Subscribe(roomId);

        try
        {
            await foreach (var payload in subscription.Reader.ReadAllAsync(ct))
            {
                yield return ResponseMapper.ToResponse(payload);
            }
        }
        finally
        {
            subscription.Dispose();
        }
    }
}
