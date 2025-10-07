using System.Threading;
using System.Threading.Tasks;

namespace CoffeeTalk.Domain.CoffeeBars;

public interface ICoffeeBarRepository
{
    Task<CoffeeBar?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);

    Task AddAsync(CoffeeBar coffeeBar, CancellationToken cancellationToken = default);

    Task UpdateAsync(CoffeeBar coffeeBar, CancellationToken cancellationToken = default);

    Task<bool> CodeExistsAsync(CoffeeBarCode code, CancellationToken cancellationToken = default);
}
