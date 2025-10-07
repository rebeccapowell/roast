namespace CoffeeTalk.Domain.CoffeeBars;

public sealed class Hipster
{
    internal Hipster(Guid id, string username, string normalizedUsername, int maxIngredientQuota)
    {
        Id = id;
        Username = username;
        NormalizedUsername = normalizedUsername;
        MaxIngredientQuota = maxIngredientQuota;
    }

    public Guid Id { get; }

    public string Username { get; }

    public string NormalizedUsername { get; }

    public int MaxIngredientQuota { get; private set; }

    internal void UpdateQuota(int quota)
    {
        if (quota < 1)
        {
            throw new DomainException("Hipster ingredient quota must be at least one.");
        }

        MaxIngredientQuota = quota;
    }

    internal static Hipster FromState(Guid id, string username, string normalizedUsername, int maxIngredientQuota)
    {
        var hipster = new Hipster(id, username, normalizedUsername, maxIngredientQuota);
        hipster.UpdateQuota(maxIngredientQuota);
        return hipster;
    }
}
