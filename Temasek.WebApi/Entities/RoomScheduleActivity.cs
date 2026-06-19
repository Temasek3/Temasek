namespace Temasek.WebApi.Features.Rooms.Schedule;

public sealed record RoomScheduleActivity(
    string Id,
    string Title,
    string Start,
    string End,
    string Personnel,
    string Location
);
