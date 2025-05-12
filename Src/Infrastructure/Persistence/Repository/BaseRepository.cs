using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using TeraLinkaMSDocEditorApi.Domain.Interfaces;
using TeraLinkaMSDocEditorApi.Web.Extensions;

namespace TeraLinkaMSDocEditorApi.Infrastructure.Persistence.Repository;

// 存儲庫的泛型實現
public class GenericRepository<T, C> : IRepository<T, C>
    where T : class
    where C : DbContext
{
    protected readonly C _context;
    protected readonly DbSet<T> _dbSet;

    // 使用EF Core的DbContext初始化存儲庫
    public GenericRepository(C context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    public DbSet<T> GetDBSet()
    {
        return _dbSet;
    }

    // 添加實體到DbContext
    public async Task AddAsync(T entity)
    {
        await _dbSet.AddAsync(entity);
    }

    // 更新實體
    public async Task UpdateAsync(T entity)
    {
        _dbSet.Attach(entity);
        _context.Entry(entity).State = EntityState.Modified;
        await Task.CompletedTask;
    }

    // 只更新指定的屬性
    public async Task UpdatePartialAsync(T entity, params string[] propertiesToUpdate)
    {
        _dbSet.Attach(entity);
        var entry = _context.Entry(entity);

        // 獲取實體類型的主鍵屬性
        var keyProperties = _context.Model
            .FindEntityType(typeof(T))
            .FindPrimaryKey()
            .Properties.Select(p => p.Name)
            .ToHashSet();

        // 只標記指定的屬性為已修改（排除主鍵）
        foreach (var propertyName in propertiesToUpdate)
        {
            // 跳過主鍵屬性
            if (!keyProperties.Contains(propertyName))
            {
                entry.Property(propertyName).IsModified = true;
            }
        }

        await Task.CompletedTask;
    }

    public async Task DeleteByIdAsync(object id)
    {
        T entity = await _dbSet.FindAsync(id);
        if (entity != null)
        {
            _dbSet.Remove(entity);
        }
    }

    public async Task DeleteByConditionAsync(Expression<Func<T, bool>> expression)
    {
        var entities = await _dbSet.Where(expression).ToListAsync();
        if (entities != null)
        {
            _dbSet.RemoveRange(entities);
        }
    }

    public async Task<IEnumerable<T>> GetAllAsync(string? orderBy = null)
    {
        IQueryable<T> query = _dbSet;
        if (!string.IsNullOrEmpty(orderBy))
        {
            // 使用反射來創建按條件排序的表達式
            var type = typeof(T);
            var property = type.GetProperty(
                orderBy,
                BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance
            );
            var parameter = Expression.Parameter(type, "p");
            var propertyAccess = Expression.MakeMemberAccess(parameter, property);
            var orderByExp = Expression.Lambda(propertyAccess, parameter);

            query = query.Provider.CreateQuery<T>(
                Expression.Call(
                    typeof(Queryable),
                    "OrderBy",
                    new Type[] { type, property.PropertyType },
                    query.Expression,
                    Expression.Quote(orderByExp)
                )
            );
        }

        return await query.ToListAsync();
    }

    public async Task<T?> GetByIdAsync(object id)
    {
        return await _dbSet.FindAsync(id);
    }

    // 更彈性的查詢方法，允許外部指定查詢條件
    public async Task<IEnumerable<T>> GetByConditionAsync(
        Expression<Func<T, bool>> expression,
        Expression<Func<T, object>>? orderBy = null
    )
    {
        if (orderBy != null)
            return await _dbSet.Where(expression).AsNoTracking().OrderBy(orderBy).ToListAsync();

        return await _dbSet.Where(expression).ToListAsync();
    }

    // 更彈性的查詢方法，允許外部指定查詢條件
    public async Task<T> GetOneByConditionAsync(
        Expression<Func<T, bool>> expression,
        Expression<Func<T, object>>? orderBy = null,
        bool isDesc = false
    )
    {
        var query = _dbSet.Where(expression).AsNoTracking();

        if (orderBy != null)
        {
            query = isDesc ? query.OrderByDescending(orderBy) : query.OrderBy(orderBy);
        }

        return await query.Take(1).FirstAsync();
    }

    public IQueryable GetQueryAsync(
        Expression<Func<T, bool>> expression,
        Expression<Func<T, object>>? orderBy = null,
        bool isDesc = false
    )
    {
        var query = _dbSet.Where(expression).AsNoTracking();

        if (orderBy != null)
        {
            query = isDesc ? query.OrderByDescending(orderBy) : query.OrderBy(orderBy);
        }

        return query;
    }

    // Upsert
    public async Task UpsertAsync(T entity, object id)
    {
        var existingEntity = await _dbSet.FindAsync(id);
        if (existingEntity == null)
        {
            // 如果實體不存在，則添加
            await _dbSet.AddAsync(entity);
        }
        else
        {
            // 如果實體已存在，則更新
            _context.Entry(existingEntity).CurrentValues.SetValues(entity);
        }
    }

    public async Task UpsertConditionAsync(T entity, Expression<Func<T, bool>> expression)
    {
        var existingEntity = await _dbSet.FirstOrDefaultAsync(expression); // 使用表達式樹尋找實體
        if (existingEntity == null)
        {
            // 如果實體不存在，則添加
            await _dbSet.AddAsync(entity);
        }
        else
        {
            // 如果實體已存在，則更新
            _context.Entry(existingEntity).CurrentValues.SetValues(entity);
        }
    }

    public async Task<List<T>> GetFromQueryStr(string? filterStr)
    {
        if (string.IsNullOrEmpty(filterStr))
            return await _dbSet.ToListAsync();

        var queryable = (IQueryable<T>)_dbSet;
        return await queryable.FilterFromQueryString(filterStr).ToListAsync();
    }
}