using System.Linq.Expressions;
using System.Reflection;
using EventHub.Application.Interfaces.Persistence;

namespace EventHub.Tests.Fakes;

public class InMemoryRepository<T> : IGenericRepository<T> where T : class
{
    private readonly List<T> _items = [];
    private int _nextId = 1;

    public IReadOnlyList<T> Items => _items;

    public Task<T?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = _items.FirstOrDefault(item => GetKey(item) == id);
        return Task.FromResult(entity);
    }

    public Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<T>>(_items.ToList());

    public Task<IReadOnlyList<T>> FindAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        var compiled = predicate.Compile();
        return Task.FromResult<IReadOnlyList<T>>(_items.Where(compiled).ToList());
    }

    public Task<T?> FirstOrDefaultAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        var compiled = predicate.Compile();
        return Task.FromResult(_items.FirstOrDefault(compiled));
    }

    public Task<bool> AnyAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        var compiled = predicate.Compile();
        return Task.FromResult(_items.Any(compiled));
    }

    public virtual Task AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        AssignIdIfNeeded(entity);
        _items.Add(entity);
        return Task.CompletedTask;
    }

    public void Update(T entity)
    {
        // Entities are mutated in place; keep the same instance if already tracked.
        if (_items.Contains(entity))
            return;

        var key = GetKey(entity);
        var existing = _items.FirstOrDefault(item => GetKey(item) == key);
        if (existing is not null)
        {
            var index = _items.IndexOf(existing);
            _items[index] = entity;
        }
        else
        {
            _items.Add(entity);
        }
    }

    public void Remove(T entity) => _items.Remove(entity);

    public void Seed(params T[] entities)
    {
        foreach (var entity in entities)
        {
            AssignIdIfNeeded(entity);
            _items.Add(entity);
        }
    }

    private void AssignIdIfNeeded(T entity)
    {
        var idProperty = typeof(T).GetProperty("Id", BindingFlags.Public | BindingFlags.Instance);
        if (idProperty is null || idProperty.PropertyType != typeof(int) || !idProperty.CanWrite)
            return;

        if ((int)idProperty.GetValue(entity)! != 0)
        {
            _nextId = Math.Max(_nextId, (int)idProperty.GetValue(entity)! + 1);
            return;
        }

        idProperty.SetValue(entity, _nextId++);
    }

    private static int GetKey(T entity)
    {
        var type = typeof(T);
        var idProperty = type.GetProperty("Id", BindingFlags.Public | BindingFlags.Instance);
        if (idProperty is not null && idProperty.PropertyType == typeof(int))
            return (int)idProperty.GetValue(entity)!;

        var userIdProperty = type.GetProperty("UserId", BindingFlags.Public | BindingFlags.Instance);
        if (userIdProperty is not null && userIdProperty.PropertyType == typeof(int))
            return (int)userIdProperty.GetValue(entity)!;

        throw new InvalidOperationException($"{type.Name} has no Id or UserId key.");
    }
}
