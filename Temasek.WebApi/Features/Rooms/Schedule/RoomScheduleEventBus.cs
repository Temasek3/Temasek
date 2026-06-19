using System.Collections.Concurrent;
using System.Threading.Channels;
using Microsoft.Extensions.Options;
using Temasek.WebApi.Entities;

namespace Temasek.WebApi.Features.Rooms.Schedule;

public class RoomScheduleEventBus(
    ILogger<RoomScheduleEventBus> logger,
    IOptions<RoomScheduleEventBusOptions> options
)
{
    private readonly ILogger<RoomScheduleEventBus> logger = logger;
    private readonly int subscriberBufferCapacity = Math.Max(
        1,
        options.Value.SubscriberBufferCapacity
    );

    private readonly ConcurrentDictionary<
        RoomId,
        ConcurrentDictionary<Guid, Channel<Room>>
    > subscribers = new();

    public Subscription Subscribe(RoomId roomId)
    {
        var channel = Channel.CreateBounded<Room>(
            new BoundedChannelOptions(subscriberBufferCapacity)
            {
                FullMode = BoundedChannelFullMode.DropOldest,
                SingleReader = true,
                SingleWriter = false,
            }
        );
        var roomSubscribers = subscribers.GetOrAdd(
            roomId,
            _ => new ConcurrentDictionary<Guid, Channel<Room>>()
        );
        var subscriptionId = Guid.NewGuid();

        roomSubscribers[subscriptionId] = channel;

        logger.LogDebug(
            "Subscribed room schedule stream. RoomId: {RoomId}, SubscriptionId: {SubscriptionId}, RoomSubscriberCount: {RoomSubscriberCount}",
            roomId.Value,
            subscriptionId,
            roomSubscribers.Count
        );

        return new Subscription(
            channel.Reader,
            () => Unsubscribe(roomId, subscriptionId, "disposed")
        );
    }

    public void Publish(Room payload)
    {
        if (!subscribers.TryGetValue(payload.RoomId, out var roomSubscribers))
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
            Unsubscribe(payload.RoomId, subscriptionId, "write-failed");
        }
    }

    private void Unsubscribe(RoomId roomId, Guid subscriptionId, string reason)
    {
        if (!subscribers.TryGetValue(roomId, out var roomSubscribers))
        {
            return;
        }

        if (roomSubscribers.TryRemove(subscriptionId, out var channel))
        {
            channel.Writer.TryComplete();

            logger.LogDebug(
                "Unsubscribed room schedule stream. RoomId: {RoomId}, SubscriptionId: {SubscriptionId}, Reason: {Reason}, RoomSubscriberCount: {RoomSubscriberCount}",
                roomId.Value,
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

    public readonly record struct Subscription(ChannelReader<Room> Reader, Action Dispose);
}
