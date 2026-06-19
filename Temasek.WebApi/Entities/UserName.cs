namespace Temasek.WebApi.Entities;

public sealed record UserName
{
    public static UserName Empty { get; } = new(string.Empty);

    public string Value { get; }

    private UserName(string value)
    {
        Value = value;
    }

    public bool IsEmpty => string.IsNullOrEmpty(Value);

    public static UserName From(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? Empty : new UserName(value.Trim());
    }

    public override string ToString() => Value;
}
