namespace Temasek.WebApi.Features.Rooms.Signboard;

public class RoomSignboardEventBusOptions
{
    public int SubscriberBufferCapacity { get; set; } = 32;
}
