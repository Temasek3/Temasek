using Riok.Mapperly.Abstractions;
using Temasek.WebApi.Entities;

namespace Temasek.WebApi.Features.Rooms.Schedule.Get;

[Mapper]
public static partial class ResponseMapper
{
    [MapProperty(
        nameof(Room.Schedule) + "." + nameof(RoomSchedule.Activities),
        nameof(Response.Schedule)
    )]
    [MapPropertyFromSource(nameof(Response.Name), Use = nameof(MapName))]
    public static partial Response ToResponse(Room source);

    private static string MapName(Room source) => source.DisplayName;

    private static partial RoomScheduleActivityResponse ToResponse(RoomScheduleActivity source);
}
