namespace Temasek.WebApi.Features.Rooms.Schedule;

public sealed record RoomId
{
    public static RoomId Empty { get; } = new(string.Empty);

    public string Value { get; }

    private RoomId(string value)
    {
        Value = value;
    }

    public bool IsEmpty => string.IsNullOrEmpty(Value);

    public static RoomId From(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? Empty : new RoomId(value.Trim());
    }

    public override string ToString() => Value;
}
