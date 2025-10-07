namespace CoffeeTalk.Domain.CoffeeBars;

public sealed record CoffeeBarCode
{
    private static readonly HashSet<char> DisallowedCharacters =
    [
        'A', 'E', 'I', 'O', 'U'
    ];

    private CoffeeBarCode(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static CoffeeBarCode From(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException("Coffee bar code is required.");
        }

        var normalized = value.Trim().ToUpperInvariant();

        if (normalized.Length != 6)
        {
            throw new DomainException("Coffee bar code must be exactly 6 characters long.");
        }

        foreach (var character in normalized)
        {
            if (!char.IsLetterOrDigit(character))
            {
                throw new DomainException("Coffee bar code must contain only alphanumeric characters.");
            }

            if (char.IsLetter(character) && DisallowedCharacters.Contains(character))
            {
                throw new DomainException("Coffee bar code cannot contain vowels.");
            }
        }

        return new CoffeeBarCode(normalized);
    }

    public override string ToString() => Value;
}
