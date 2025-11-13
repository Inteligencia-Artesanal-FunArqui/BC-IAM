using Microsoft.EntityFrameworkCore;
using OsitoPolar.IAM.Service.Shared.Domain.Repositories;

namespace OsitoPolar.IAM.Service.Shared.Infrastructure.Persistence.EFC.Repositories;

public class UnitOfWork(DbContext context) : IUnitOfWork
{
    /// <inheritdoc />
    public async Task CompleteAsync()
    {
        await context.SaveChangesAsync();
    }
}