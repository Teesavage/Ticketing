using System.Linq.Expressions;
using Ticketing.Infrastructure.PaginationHelper;

namespace Ticketing.Infrastructure.IRespository
{
    public interface IGenericRepository<T> where T : class
    {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
        Task<IList<T>> GetAll(Expression<Func<T, bool>> expression = null,
            Func<IQueryable<T>, IOrderedQueryable<T>> orderBy = null,
            List<string> includes = null);
        Task<IList<T>> GetAll(PaginationFilter paginationFilter, List<string> includes = null);
        Task<T> Get(Expression<Func<T, bool>> expression, List<string> includes = null);
        Task<IList<T>> GetAllWithTracking(Expression<Func<T, bool>> expression = null,
            Func<IQueryable<T>, IOrderedQueryable<T>> orderBy = null,
            List<string> includes = null);
        Task<IList<T>> GetAllWithTracking(PaginationFilter paginationFilter, List<string> includes = null);
        Task<T> GetWithTracking(Expression<Func<T, bool>> expression, List<string> includes = null);
        Task<IEnumerable<T>> GetWithIncludes(Expression<Func<T, bool>>? filter = null, params Expression<Func<T, object>>[] includeProperties);
        Task Insert(T entity);
        Task InsertRange(IEnumerable<T> entities);
        Task Delete(int id);
        Task Delete(long id);
        Task DeleteGuid(Guid id);
        void DeleteRange(IEnumerable<T> entities);
        void Update(T entity);
#pragma warning disable CS0693 // Type parameter has the same name as the type parameter from outer type
        IQueryable<T> GetAllQueryable<T>() where T : class;
        Task<IEnumerable<T>> GetWithIncludeChainAsync(Func<IQueryable<T>, IQueryable<T>> includeFunc,
            Expression<Func<T, bool>> filter = null);
        Task<T> GetByIdWithIncludeChainAsync(
            Func<IQueryable<T>, IQueryable<T>> includeFunc,
            Expression<Func<T, bool>> predicate
        );
        IQueryable<T> GetAllQueryable();
    }
}
