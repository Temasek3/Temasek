namespace Temasek.WebApi.Features.Rooms.Signboard.Contracts;

public class SignboardActivityDto
{
    public string Id { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Start { get; init; } = string.Empty;
    public string End { get; init; } = string.Empty;
    public string Personnel { get; init; } = string.Empty;
    public string Location { get; init; } = string.Empty;
}
