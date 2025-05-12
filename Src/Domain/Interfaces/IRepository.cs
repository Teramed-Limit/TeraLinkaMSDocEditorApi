using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace TeraLinkaMSDocEditorApi.Domain.Interfaces;

// 更新存儲庫的接口以支持異步操作
public interface IRepository<T, C>
    where T : class
    where C : DbContext
{
    Task<T?> GetByIdAsync(object id);
    Task<List<T>> GetFromQueryStr(string? filterStr);
    Task AddAsync(T entity);
    Task UpsertAsync(T entity, object id);
    Task UpsertConditionAsync(T entity, Expression<Func<T, bool>> expression);
    Task UpdateAsync(T entity);
    Task UpdatePartialAsync(T entity, params string[] propertiesToUpdate);
    Task DeleteByIdAsync(object id);
    Task DeleteByConditionAsync(Expression<Func<T, bool>> expression);
    Task<IEnumerable<T>> GetAllAsync(string? orderBy = null);
    Task<IEnumerable<T>> GetByConditionAsync(
        Expression<Func<T, bool>> expression,
        Expression<Func<T, object>>? orderBy = null
    );
    Task<T> GetOneByConditionAsync(
        Expression<Func<T, bool>> expression,
        Expression<Func<T, object>>? orderBy = null,
        bool isDesc = false
    );
    IQueryable GetQueryAsync(
        Expression<Func<T, bool>> expression,
        Expression<Func<T, object>>? orderBy = null,
        bool isDesc = false
    );

    DbSet<T> GetDBSet();
}
