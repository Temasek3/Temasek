using Temasek.WebApi.Features.Rooms.Signboard.Contracts;

namespace Temasek.WebApi.Features.Rooms.Signboard.Update;

public class Request
{
    public string? Name { get; init; }
    public IReadOnlyList<SignboardActivityDto> Schedule { get; init; } =
        Array.Empty<SignboardActivityDto>();
}
