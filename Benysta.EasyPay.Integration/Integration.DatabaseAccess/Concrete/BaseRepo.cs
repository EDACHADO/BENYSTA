using Integration.DatabaseAccess.Context;
using Integration.Infrastructures.Abstractions;
using Integration.Models.AbstractModel;
using Integration.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace Integration.DatabaseAccess.Concrete;

public class BaseRepo<T> : IRepo<T> where T : BaseAudit
{
    public char[] Separator = [','];
    private readonly DbSet<T> _dbSet;

    public BaseRepo(NibssNpsDbContext db)
    {
        Db = db;
        _dbSet = Db.Set<T>();
    }

    public NibssNpsDbContext Db { get; set; }

    public IQueryable<T> GetAllByQuery(Expression<Func<T, bool>> filter = null)
    {
        var query = _dbSet.AsNoTracking();

        if (filter != null) query = query.Where(filter);
        return query;
    }


    public IQueryable<T> GetAllByQuery(string includeProperties = "", Expression<Func<T, bool>> filter = null)
    {
        var query = _dbSet.AsNoTracking();
        if (filter != null) query = query.Where(filter);

        if (includeProperties != null) query = includeProperties.Split(Separator, StringSplitOptions.RemoveEmptyEntries)
                            .Aggregate(query, (current, includeProperty) => current.Include(includeProperty));
        return query;
    }

    public IQueryable<T> GetAllByFilterQuery(ICollection<Expression<Func<T, bool>>> filters = null)
    {
        var query = _dbSet.AsNoTracking();

        if (filters != null)
            query = filters.Aggregate(query, (current, filter) => current.Where(filter));


        return query;
    }

    public IQueryable<T> GetAllByQueryPagination(int skip, int pageSize, Expression<Func<T, bool>> filter = null, string includeProperties = "")
    {
        var query = _dbSet.AsNoTracking();

        if (includeProperties != null) query = includeProperties.Split(Separator, StringSplitOptions.RemoveEmptyEntries)
                                .Aggregate(query, (current, includeProperty) => current.Include(includeProperty));

        if (filter != null) query = query.Where(filter);

        return query.Skip(skip).Take(pageSize);
    }

    public IQueryable<T> GetAllByQueriesPagination(int skip, int pageSize, ICollection<Expression<Func<T, bool>>> filters = null, string includeProperties = "")
    {
        var query = _dbSet.AsNoTracking();

        if (includeProperties != null) query = includeProperties.Split(Separator, StringSplitOptions.RemoveEmptyEntries)
            .Aggregate(query, (current, includeProperty) => current.Include(includeProperty));

        if (filters != null)
            query = filters.Aggregate(query, (current, filter) => current.Where(filter));

        return query.Skip(skip).Take(pageSize);
    }

    public Task<int> CountAsync(Expression<Func<T, bool>> filter = null)
    {
        var query = _dbSet.AsNoTracking();


        if (filter != null) query = query.Where(filter);

        return query.CountAsync();
    }

    public Task<int> CountFiltersAsync(ICollection<Expression<Func<T, bool>>> filters = null)
    {
        var query = _dbSet.AsNoTracking();

        if (filters != null)
            query = filters.Aggregate(query, (current, filter) => current.Where(filter));

        return query.CountAsync();
    }


    public async Task<T> GetFirstOrDefaultAsync(Expression<Func<T, bool>> filter = null, string includeProperties = "")
    {
        var query = _dbSet.AsNoTracking();

        if (includeProperties != null)
            foreach (var includeProperty in includeProperties.Split
                         (Separator, StringSplitOptions.RemoveEmptyEntries))
                query = query.Include(includeProperty);

        if (filter != null) query = query.Where(filter);
        return await query.FirstOrDefaultAsync();
    }

    public async Task<bool> CheckQueryAsync(Expression<Func<T, bool>> filter, string includeProperties = "")
    {
        var query = _dbSet.AsNoTracking();

        if (includeProperties != null)
            foreach (var includeProperty in includeProperties.Split
                         (Separator, StringSplitOptions.RemoveEmptyEntries))
                query = query.Include(includeProperty);
        return await query.AnyAsync(filter);
    }

    public async Task<T> GetById(object id)
    {
        return await _dbSet.FindAsync(id);
    }

    public async Task AddRecord(T t)
    {
        await Db.AddAsync(t);
    }

    public async Task AddRecordRange(List<T> t)
    {
        await Db.AddRangeAsync(t);
    }

    public void UpdateRecord(T t)
    {
        if (Db.Entry(t).State == EntityState.Detached)
        {
            _dbSet.Attach(t);
        }
        Db.Entry(t).State = EntityState.Modified;
    }
    public void DetachRecord(T t)
    {
        Db.Entry(t).State = EntityState.Detached;
    }

    public async Task<ResponseVm> SaveContextAsync()
    {
        try
        {
            await Db.SaveChangesAsync();
            return ResponseVm.Success("Committed Successfully");
        }
        catch (Exception e)
        {
            return ResponseVm.Failed(e.InnerException?.Message);
        }
    }


    public async Task Delete(object id)
    {
        var t = await _dbSet.FindAsync(id);
        if (t is null)
            return;

        if (Db.Entry(t).State == EntityState.Detached) _dbSet.Attach(t);
        _dbSet.Remove(t);
    }

    public void Remove(T t)
    {
        _dbSet.Remove(t);
    }

    public async Task<bool> CheckAny(int id)
    {
        var t = await _dbSet.FindAsync(id);
        return t != null;
    }
}