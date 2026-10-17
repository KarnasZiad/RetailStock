namespace RetailStock.Application.Abstractions;

public interface IUnitOfWork
{
    /// <summary>Persists all pending changes atomically.</summary>
    /// <exception cref="Common.ConcurrencyConflictException">Another transaction modified the same data.</exception>
    Task CommitAsync(CancellationToken cancellationToken = default);

    /// <summary>Forgets every pending (uncommitted) change, e.g. before retrying an operation.</summary>
    void DiscardChanges();
}
