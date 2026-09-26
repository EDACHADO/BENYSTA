using Integration.Models;
using Integration.Models.AbstractModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Integration.DatabaseAccess.Context;

public static class DbContextHelper
{
    public static void SoftDeleteAutomaticBuilder(ModelBuilder builder)
    {
        foreach (var entityType in builder.Model.GetEntityTypes())
            //other automated configurations left out
            if (typeof(ISoftDelete).IsAssignableFrom(entityType.ClrType))
                entityType.AddSoftDeleteQueryFilter();
    }
    public static void UniqueKeyAutomaticBuilder(ModelBuilder builder)
    {
        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            #region Convert UniqueKeyAttribute on Entities to UniqueKey in DB

            var properties = entityType.GetProperties().ToList();
            foreach (var property in properties)
            {
                var uniqueKeys = GetUniqueKeyAttributes(entityType, property);
                if (uniqueKeys != null)
                {
                    foreach (var uniqueKey in uniqueKeys.Where(x => x.Order == 0))
                        // Single column Unique Key
                        if (string.IsNullOrWhiteSpace(uniqueKey.GroupId))
                        {
                            entityType.AddIndex(property).IsUnique = true;
                        }
                        // Multiple column Unique Key
                        else
                        {
                            var mutableProperties = new List<IMutableProperty>();
                            properties.ToList().ForEach(x =>
                            {
                                var uks = GetUniqueKeyAttributes(entityType, x);
                                if (uks != null) mutableProperties.AddRange(from uk in uks where uk != null && uk.GroupId == uniqueKey.GroupId select x);
                            });
                            entityType.AddIndex(mutableProperties).IsUnique = true;
                        }
                }
            }

            #endregion Convert UniqueKeyAttribute on Entities to UniqueKey in DB
        }
    }

    private static IEnumerable<UniqueKeyAttribute> GetUniqueKeyAttributes(IMutableEntityType entityType, IMutableProperty property)
    {
        var propInfo = entityType.ClrType.GetProperty(
            property.Name,
            BindingFlags.NonPublic |
            BindingFlags.Public |
            BindingFlags.Static |
            BindingFlags.Instance |
            BindingFlags.DeclaredOnly);
        return propInfo?.GetCustomAttributes<UniqueKeyAttribute>();
    }
}