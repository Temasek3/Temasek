namespace Temasek.WebApi.Entities;

public sealed record UserNric
{
    public static UserNric Empty { get; } = new(string.Empty);

    public string Value { get; }

    private UserNric(string value)
    {
        Value = value;
    }

    public bool IsEmpty => string.IsNullOrEmpty(Value);

    public static UserNric From(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? Empty : new UserNric(value.Trim());
    }

    public override string ToString() => Value;
}
