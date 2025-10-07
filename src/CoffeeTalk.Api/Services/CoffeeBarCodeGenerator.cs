using System.Security.Cryptography;
using CoffeeTalk.Domain.CoffeeBars;

namespace CoffeeTalk.Api.Services;

public sealed class CoffeeBarCodeGenerator(ICoffeeBarRepository repository) : ICoffeeBarCodeGenerator
{
    private const string AllowedCharacters = "BCDFGHJKLMNPQRSTVWXYZ0123456789";
    private const int CodeLength = 6;
    private readonly ICoffeeBarRepository _repository = repository;

    public async Task<string> GenerateAsync(CancellationToken cancellationToken = default)
    {
        for (var attempt = 0; attempt < 128; attempt++)
        {
            var candidate = GenerateCandidate();
            var code = CoffeeBarCode.From(candidate);

            if (!await _repository.CodeExistsAsync(code, cancellationToken).ConfigureAwait(false))
            {
                return code.Value;
            }
        }

        throw new InvalidOperationException("Unable to generate a unique coffee bar code.");
    }

    private static string GenerateCandidate()
    {
        Span<char> buffer = stackalloc char[CodeLength];
        var span = AllowedCharacters.AsSpan();

        Span<byte> randomBytes = stackalloc byte[CodeLength];
        RandomNumberGenerator.Fill(randomBytes);

        for (var i = 0; i < CodeLength; i++)
        {
            var index = randomBytes[i] % span.Length;
            buffer[i] = span[index];
        }

        return new string(buffer);
    }
}
