using Riok.Mapperly.Abstractions;

namespace Temasek.WebApi.Features.Rooms.Schedule.Update;

[Mapper]
public static partial class RequestMapper
{
    public static RoomSchedule ToSchedule(Request source)
    {
        var activities = source.Schedule.Select(ToActivity).ToArray();
        return activities.Length == 0 ? RoomSchedule.Empty : new RoomSchedule(activities);
    }

    private static partial RoomScheduleActivity ToActivity(RoomScheduleActivityRequest source);
}
