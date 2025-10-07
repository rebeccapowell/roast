namespace CoffeeTalk.Api.Services;

public interface ICoffeeBarCodeGenerator
{
    Task<string> GenerateAsync(CancellationToken cancellationToken = default);
}
