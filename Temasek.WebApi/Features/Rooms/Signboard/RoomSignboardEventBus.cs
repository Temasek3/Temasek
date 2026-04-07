using System.Collections.Concurrent;
using System.Threading.Channels;
using Microsoft.Extensions.Options;
using Temasek.WebApi.Features.Rooms.Signboard.Contracts;

namespace Temasek.WebApi.Features.Rooms.Signboard;

public class RoomSignboardEventBus(
    ILogger<RoomSignboardEventBus> logger,
    IOptions<RoomSignboardEventBusOptions> options
)
{
    private readonly ILogger<RoomSignboardEventBus> logger = logger;
    private readonly int subscriberBufferCapacity = Math.Max(
        1,
        options.Value.SubscriberBufferCapacity
    );

    private readonly ConcurrentDictionary<
        string,
        ConcurrentDictionary<Guid, Channel<RoomSignboardResponse>>
    > subscribers = new(StringComparer.OrdinalIgnoreCase);

    public Subscription Subscribe(string roomId)
    {
        var normalizedRoomId = NormalizeRoomId(roomId);
        var channel = Channel.CreateBounded<RoomSignboardResponse>(
            new BoundedChannelOptions(subscriberBufferCapacity)
            {
                FullMode = BoundedChannelFullMode.DropOldest,
                SingleReader = true,
                SingleWriter = false,
            }
        );
        var roomSubscribers = subscribers.GetOrAdd(
            normalizedRoomId,
            _ => new ConcurrentDictionary<Guid, Channel<RoomSignboardResponse>>()
        );
        var subscriptionId = Guid.NewGuid();

        roomSubscribers[subscriptionId] = channel;

        logger.LogDebug(
            "Subscribed room signboard stream. RoomId: {RoomId}, SubscriptionId: {SubscriptionId}, RoomSubscriberCount: {RoomSubscriberCount}",
            normalizedRoomId,
            subscriptionId,
            roomSubscribers.Count
        );

        return new Subscription(
            channel.Reader,
            () => Unsubscribe(normalizedRoomId, subscriptionId, "disposed")
        );
    }

    public void Publish(RoomSignboardResponse payload)
    {
        var normalizedRoomId = NormalizeRoomId(payload.RoomId);
        if (!subscribers.TryGetValue(normalizedRoomId, out var roomSubscribers))
        {
            return;
        }

        List<Guid>? failedSubscriptions = null;

        foreach (var subscriber in roomSubscribers)
        {
            if (!subscriber.Value.Writer.TryWrite(payload))
            {
                failedSubscriptions ??= [];
                failedSubscriptions.Add(subscriber.Key);
            }
        }

        if (failedSubscriptions is null)
        {
            return;
        }

        foreach (var subscriptionId in failedSubscriptions)
        {
            Unsubscribe(normalizedRoomId, subscriptionId, "write-failed");
        }
    }

    private void Unsubscribe(string roomId, Guid subscriptionId, string reason)
    {
        if (!subscribers.TryGetValue(roomId, out var roomSubscribers))
        {
            return;
        }

        if (roomSubscribers.TryRemove(subscriptionId, out var channel))
        {
            channel.Writer.TryComplete();

            logger.LogDebug(
                "Unsubscribed room signboard stream. RoomId: {RoomId}, SubscriptionId: {SubscriptionId}, Reason: {Reason}, RoomSubscriberCount: {RoomSubscriberCount}",
                roomId,
                subscriptionId,
                reason,
                roomSubscribers.Count
            );
        }

        if (
            roomSubscribers.IsEmpty
            && subscribers.TryGetValue(roomId, out var currentRoomSubscribers)
            && ReferenceEquals(currentRoomSubscribers, roomSubscribers)
        )
        {
            subscribers.TryRemove(roomId, out _);
        }
    }

    private static string NormalizeRoomId(string roomId)
    {
        return string.IsNullOrWhiteSpace(roomId) ? string.Empty : roomId.Trim();
    }

    public readonly record struct Subscription(
        ChannelReader<RoomSignboardResponse> Reader,
        Action Dispose
    );
}
