using System.Runtime.CompilerServices;
using FastEndpoints;

namespace Temasek.WebApi.Features.Rooms.Signboard.Stream;

public class Endpoint(RoomSignboardStore store, RoomSignboardEventBus eventBus)
    : EndpointWithoutRequest
{
    private const string SignboardEventName = "signboard";

    public override void Configure()
    {
        Get("/rooms/{roomId}/signboard/stream");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var roomId = Route<string>("roomId") ?? string.Empty;

        HttpContext.Response.Headers["Cache-Control"] = "no-cache";
        HttpContext.Response.Headers.Append("X-Accel-Buffering", "no");

        await Send.EventStreamAsync(SignboardEventName, GetSignboardStream(roomId, ct), ct);
    }

    private async IAsyncEnumerable<Contracts.RoomSignboardResponse> GetSignboardStream(
        string roomId,
        [EnumeratorCancellation] CancellationToken ct
    )
    {
        yield return await store.GetAsync(roomId, ct);

        var subscription = eventBus.Subscribe(roomId);

        try
        {
            await foreach (var payload in subscription.Reader.ReadAllAsync(ct))
            {
                yield return payload;
            }
        }
        finally
        {
            subscription.Dispose();
        }
    }
}
