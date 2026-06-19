namespace Temasek.WebApi.Features.Rooms.Schedule;

public sealed record RoomSchedule(IReadOnlyList<RoomScheduleActivity> Activities)
{
    public static RoomSchedule Empty { get; } = new([]);

    public int Count => Activities.Count;
}
