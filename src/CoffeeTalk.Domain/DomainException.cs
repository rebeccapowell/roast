namespace CoffeeTalk.Domain;

public class DomainException : InvalidOperationException
{
    public DomainException(string message)
        : base(message)
    {
    }
}
