namespace Temasek.Tests;

public sealed class CreateRoomRequest
{
    public required string RoomId { get; init; }
    public string? Name { get; init; }
}

public sealed class RoomSummaryResponse
{
    public string RoomId { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public int ScheduleCount { get; init; }
    public DateTime UpdatedAtUtc { get; init; }
}

public sealed class UpdateRoomScheduleRequest
{
    public string? Name { get; init; }
    public IReadOnlyList<RoomScheduleActivityDto> Schedule { get; init; } = [];
}

public sealed class RoomScheduleResponse
{
    public string RoomId { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public IReadOnlyList<RoomScheduleActivityDto> Schedule { get; init; } = [];
    public DateTime UpdatedAtUtc { get; init; }
}

public sealed class RoomScheduleActivityDto
{
    public string Id { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Start { get; init; } = string.Empty;
    public string End { get; init; } = string.Empty;
    public string Personnel { get; init; } = string.Empty;
    public string Location { get; init; } = string.Empty;
}
