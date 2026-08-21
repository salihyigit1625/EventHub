using EventHub.Application.Interfaces.Persistence;
using EventHub.Repository.Context;
using Microsoft.EntityFrameworkCore;

namespace EventHub.Repository.Persistence;

public class UnitOfWork(AppDbContext context) : IUnitOfWork
{
    private readonly Dictionary<Type, object> _repositories = new();

    public IGenericRepository<TEntity> Repository<TEntity>() where TEntity : class
    {
        var type = typeof(TEntity);

        if (_repositories.TryGetValue(type, out var repository))
            return (IGenericRepository<TEntity>)repository;

        var genericRepository = new GenericRepository<TEntity>(context);
        _repositories[type] = genericRepository;
        return genericRepository;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new InvalidOperationException(
                "The data was modified by another request. Please retry.");
        }
    }

    public void Dispose() => context.Dispose();
}
