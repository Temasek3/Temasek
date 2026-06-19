using FreeSql.DataAnnotations;
using Temasek.WebApi.Features.Rooms.Schedule;

namespace Temasek.WebApi.Entities;

public class Room
{
    [Column(IsPrimary = true, IsIdentity = true)]
    public RoomId RoomId { get; set; } = RoomId.Empty;

    public RoomName Name { get; set; } = RoomName.Empty;

    [Column(Name = "ScheduleJson")]
    public RoomSchedule Schedule { get; set; } = RoomSchedule.Empty;

    public DateTime UpdatedAtUtc { get; set; }

    public string DisplayName => !Name.IsEmpty ? Name.Value : RoomName.From(RoomId.Value).Value;
}
