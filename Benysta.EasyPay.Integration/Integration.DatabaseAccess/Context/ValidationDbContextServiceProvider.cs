using Microsoft.EntityFrameworkCore;

namespace Integration.DatabaseAccess.Context;

public class ValidationDbContextServiceProvider(DbContext currContext) : IServiceProvider
{
    public object GetService(Type serviceType)
    {
        return serviceType == typeof(DbContext) ? currContext : (object)null;
    }
}