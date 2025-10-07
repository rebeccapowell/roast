using System.Collections.Generic;
using System.Linq;
using CoffeeTalk.Domain.CoffeeBars;
using CoffeeTalk.Infrastructure.Data.Entities;
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

        var tracked = await _dbContext.CoffeeBars
            .Include(bar => bar.Hipsters)
            .Include(bar => bar.Ingredients)
            .Include(bar => bar.Submissions)
            .Include(bar => bar.Sessions)
                .ThenInclude(session => session.Cycles)
                    .ThenInclude(cycle => cycle.Votes)
            .FirstOrDefaultAsync(bar => bar.Id == coffeeBar.Id, cancellationToken)
            .ConfigureAwait(false);

        if (tracked is null)
        {
            throw new InvalidOperationException($"Coffee bar with id '{coffeeBar.Id}' was not found.");
        }

        var entity = CoffeeBarMapper.ToEntity(coffeeBar);
        ApplyUpdatedState(tracked, entity);
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public Task<bool> CodeExistsAsync(CoffeeBarCode code, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(code);

        return _dbContext.CoffeeBars.AnyAsync(bar => bar.Code == code.Value, cancellationToken);
    }

    private void ApplyUpdatedState(CoffeeBarEntity tracked, CoffeeBarEntity updated)
    {
        _dbContext.Entry(tracked).CurrentValues.SetValues(updated);

        SyncCollection(
            tracked.Hipsters,
            updated.Hipsters,
            hipster => hipster.Id,
            (existing, incoming) => _dbContext.Entry(existing).CurrentValues.SetValues(incoming),
            hipster => _dbContext.Entry(hipster).State = EntityState.Added);

        SyncCollection(
            tracked.Ingredients,
            updated.Ingredients,
            ingredient => ingredient.Id,
            (existing, incoming) => _dbContext.Entry(existing).CurrentValues.SetValues(incoming),
            ingredient => _dbContext.Entry(ingredient).State = EntityState.Added);

        SyncCollection(
            tracked.Submissions,
            updated.Submissions,
            submission => submission.Id,
            (existing, incoming) => _dbContext.Entry(existing).CurrentValues.SetValues(incoming),
            submission => _dbContext.Entry(submission).State = EntityState.Added);

        SyncCollection(
            tracked.Sessions,
            updated.Sessions,
            session => session.Id,
            (existingSession, incomingSession) =>
            {
                _dbContext.Entry(existingSession).CurrentValues.SetValues(incomingSession);

                SyncCollection(
                    existingSession.Cycles,
                    incomingSession.Cycles,
                    cycle => cycle.Id,
                    (existingCycle, incomingCycle) =>
                    {
                        _dbContext.Entry(existingCycle).CurrentValues.SetValues(incomingCycle);

                        SyncCollection(
                            existingCycle.Votes,
                            incomingCycle.Votes,
                            vote => vote.Id,
                            (existingVote, incomingVote) =>
                                _dbContext.Entry(existingVote).CurrentValues.SetValues(incomingVote),
                            vote => _dbContext.Entry(vote).State = EntityState.Added);
                    },
                    cycle => _dbContext.Entry(cycle).State = EntityState.Added);
            },
            session => _dbContext.Entry(session).State = EntityState.Added);
    }

    private static void SyncCollection<TEntity>(
        ICollection<TEntity> trackedEntities,
        IEnumerable<TEntity> updatedEntities,
        Func<TEntity, Guid> keySelector,
        Action<TEntity, TEntity> updateAction,
        Action<TEntity>? addAction = null)
        where TEntity : class
    {
        ArgumentNullException.ThrowIfNull(trackedEntities);
        ArgumentNullException.ThrowIfNull(updatedEntities);
        ArgumentNullException.ThrowIfNull(keySelector);
        ArgumentNullException.ThrowIfNull(updateAction);

        var updatedList = updatedEntities.ToList();
        var updatedLookup = updatedList.ToDictionary(keySelector);

        foreach (var tracked in trackedEntities.ToList())
        {
            if (!updatedLookup.ContainsKey(keySelector(tracked)))
            {
                trackedEntities.Remove(tracked);
            }
        }

        var trackedLookup = trackedEntities.ToDictionary(keySelector);

        foreach (var updated in updatedList)
        {
            if (trackedLookup.TryGetValue(keySelector(updated), out var existing))
            {
                updateAction(existing, updated);
            }
            else
            {
                trackedEntities.Add(updated);
                addAction?.Invoke(updated);
            }
        }
    }
}
