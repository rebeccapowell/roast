using CoffeeTalk.Domain.CoffeeBars;
using CoffeeTalk.Infrastructure.Data.Mappings;
using Microsoft.EntityFrameworkCore;

namespace CoffeeTalk.Infrastructure.Data.Repositories;

public sealed class CoffeeBarRepository(CoffeeTalkDbContext dbContext) : ICoffeeBarRepository
{
    private readonly CoffeeTalkDbContext _dbContext = dbContext;

    public async Task<CoffeeBar?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        var barCode = CoffeeBarCode.From(code);

        var entity = await _dbContext.CoffeeBars
            .AsNoTracking()
            .Include(bar => bar.Hipsters)
            .Include(bar => bar.Ingredients)
            .Include(bar => bar.Submissions)
            .Include(bar => bar.Sessions)
                .ThenInclude(session => session.Cycles)
                    .ThenInclude(cycle => cycle.Votes)
            .FirstOrDefaultAsync(bar => bar.Code == barCode.Value, cancellationToken)
            .ConfigureAwait(false);

        return entity is null ? null : CoffeeBarMapper.ToDomain(entity);
    }

    public async Task AddAsync(CoffeeBar coffeeBar, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(coffeeBar);

        var entity = CoffeeBarMapper.ToEntity(coffeeBar);
        await _dbContext.CoffeeBars.AddAsync(entity, cancellationToken).ConfigureAwait(false);
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task UpdateAsync(CoffeeBar coffeeBar, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(coffeeBar);

        var entity = CoffeeBarMapper.ToEntity(coffeeBar);
        _dbContext.CoffeeBars.Update(entity);
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public Task<bool> CodeExistsAsync(CoffeeBarCode code, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(code);

        return _dbContext.CoffeeBars.AnyAsync(bar => bar.Code == code.Value, cancellationToken);
    }
}
