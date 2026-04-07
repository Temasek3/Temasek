namespace Temasek.WebApi.Features.Rooms.List;

public class Response
{
    public string RoomId { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public int ScheduleCount { get; set; }

    public DateTime UpdatedAtUtc { get; set; }
}
