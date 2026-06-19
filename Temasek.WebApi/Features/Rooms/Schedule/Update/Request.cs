namespace Temasek.WebApi.Features.Rooms.Schedule.Update;

public class Request
{
    public string? Name { get; init; }
    public IReadOnlyList<RoomScheduleActivityRequest> Schedule { get; init; } = [];
}

public class RoomScheduleActivityRequest
{
    public string Id { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Start { get; init; } = string.Empty;
    public string End { get; init; } = string.Empty;
    public string Personnel { get; init; } = string.Empty;
    public string Location { get; init; } = string.Empty;
}
