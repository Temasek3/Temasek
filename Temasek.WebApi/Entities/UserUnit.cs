namespace Temasek.WebApi.Entities;

public sealed record UserUnit
{
    public static UserUnit Empty { get; } = new(string.Empty);

    public string Value { get; }

    private UserUnit(string value)
    {
        Value = value;
    }

    public bool IsEmpty => string.IsNullOrEmpty(Value);

    public static UserUnit From(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? Empty : new UserUnit(value.Trim());
    }

    public override string ToString() => Value;
}
