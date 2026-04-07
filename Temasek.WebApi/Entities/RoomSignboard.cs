using FreeSql.DataAnnotations;

namespace Temasek.WebApi.Entities;

public class RoomSignboard
{
    [Column(IsPrimary = true, StringLength = 120)]
    public string RoomId { get; set; } = null!;

    [Column(StringLength = 240)]
    public string Name { get; set; } = null!;

    [Column(DbType = "text")]
    public string ScheduleJson { get; set; } = "[]";

    public DateTime UpdatedAtUtc { get; set; }
}
