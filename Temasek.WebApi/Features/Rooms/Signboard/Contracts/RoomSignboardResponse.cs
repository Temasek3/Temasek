namespace Temasek.WebApi.Features.Rooms.Signboard.Contracts;

public class RoomSignboardResponse
{
    public required string RoomId { get; init; }
    public required string Name { get; init; }
    public required IReadOnlyList<SignboardActivityDto> Schedule { get; init; }
    public DateTime UpdatedAtUtc { get; init; }
}
