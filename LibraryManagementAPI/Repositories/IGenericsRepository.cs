using System.Linq.Expressions;
using LibraryManagementAPI.Models;

namespace LibraryManagementAPI.Repositories;

/// <summary>
/// Generic data access. Methods only stage changes; call <see cref="IUnitOfWork.SaveChangesAsync"/> to persist.
/// </summary>
public interface IGenericRepository<T> where T : BaseEntity
{
    Task<IEnumerable<T>> GetAllAsync();
    Task<T?> GetByIdAsync(Guid id);
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);
    Task<T> AddAsync(T entity);
    void Update(T entity);
    Task<bool> DeleteAsync(Guid id);
    Task<bool> SoftDeleteAsync(Guid id);
    Task<bool> ExistsAsync(Guid id);
}
