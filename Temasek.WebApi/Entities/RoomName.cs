namespace Temasek.WebApi.Features.Rooms.Schedule;

public sealed record RoomName
{
    public static RoomName Empty { get; } = new(string.Empty);

    public string Value { get; }

    private RoomName(string value)
    {
        Value = value;
    }

    public bool IsEmpty => string.IsNullOrEmpty(Value);

    public static RoomName From(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? Empty : new RoomName(value.Trim());
    }

    public override string ToString() => Value;
}
