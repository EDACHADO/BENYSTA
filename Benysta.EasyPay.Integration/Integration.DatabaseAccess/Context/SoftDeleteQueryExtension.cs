using Integration.Models;
using Integration.Models.AbstractModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Integration.DatabaseAccess.Context;

public static class SoftDeleteQueryExtension
{
    public static void AddSoftDeleteQueryFilter(this IMutableEntityType entityData)
    {
        var methodToCall = typeof(SoftDeleteQueryExtension).GetMethod(nameof(GetSoftDeleteFilter),
            BindingFlags.NonPublic | BindingFlags.Static)
            ?.MakeGenericMethod(entityData.ClrType);

        var filter = methodToCall?.Invoke(null, []);
        entityData.SetQueryFilter((LambdaExpression)filter);
        var entityProperty = entityData.FindProperty(nameof(ISoftDelete.SoftDeleted));
        if (entityProperty is not null)
            entityData.AddIndex(entityProperty);
    }

    private static Expression<Func<TEntity, bool>> GetSoftDeleteFilter<TEntity>()
        where TEntity : class, ISoftDelete
    {
        Expression<Func<TEntity, bool>> filter = x => !x.SoftDeleted;
        return filter;
    }
}
