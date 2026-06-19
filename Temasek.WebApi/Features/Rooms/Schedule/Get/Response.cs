namespace Temasek.WebApi.Features.Rooms.Schedule.Get;

public class Response
{
    public required string RoomId { get; init; }
    public required string Name { get; init; }
    public required IReadOnlyList<RoomScheduleActivityResponse> Schedule { get; init; }
    public DateTime UpdatedAtUtc { get; init; }
}

public class RoomScheduleActivityResponse
{
    public string Id { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Start { get; init; } = string.Empty;
    public string End { get; init; } = string.Empty;
    public string Personnel { get; init; } = string.Empty;
    public string Location { get; init; } = string.Empty;
}
