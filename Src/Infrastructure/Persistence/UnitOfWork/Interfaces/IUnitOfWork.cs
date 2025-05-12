namespace TeraLinkaMSDocEditorApi.Infrastructure.Persistence.UnitOfWork.Interfaces;

public interface IUnitOfWork : IDisposable
{
    // 提交當前單位的變更
    void Save();
    Task SaveAsync();
}
