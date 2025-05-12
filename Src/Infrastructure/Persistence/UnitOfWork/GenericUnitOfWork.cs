using Microsoft.EntityFrameworkCore;
using TeraLinkaMSDocEditorApi.Infrastructure.Persistence.UnitOfWork.Interfaces;

namespace TeraLinkaMSDocEditorApi.Infrastructure.Persistence.UnitOfWork;

public class GenericUnitOfWork<TContext> : IUnitOfWork
    where TContext : DbContext
{
    private readonly TContext _context;

    public GenericUnitOfWork(TContext context)
    {
        _context = context;
    }

    public void Save()
    {
        _context.SaveChanges();
    }

    public async Task SaveAsync()
    {
        await _context.SaveChangesAsync();
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}