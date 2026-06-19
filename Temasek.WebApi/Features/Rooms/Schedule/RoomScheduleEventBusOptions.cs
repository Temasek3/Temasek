namespace Temasek.WebApi.Features.Rooms.Schedule;

public class RoomScheduleEventBusOptions
{
    public int SubscriberBufferCapacity { get; set; } = 32;
}
