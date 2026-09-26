using Integration.DatabaseAccess.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Integration.DatabaseAccess;

/// <summary>
/// Brings the NIBSS NPS database schema up to date at startup.
/// </summary>
public static class DatabaseMigrationExtensions
{
    /// <summary>
    /// Applies every migration that has not yet been recorded in __EFMigrationsHistory.
    ///
    /// Call this after Build() and before Run(): it runs to completion before the server
    /// binds its ports, so no request can reach a controller while the schema is half a
    /// migration behind the code that queries it.
    ///
    /// A failure here is deliberately fatal. An unreachable database or a migration that
    /// cannot be applied means the process cannot serve NPS traffic correctly, and failing
    /// at startup surfaces that in the deployment rather than in the first webhook.
    /// </summary>
    public static async Task<IHost> MigrateNibssNpsDatabaseAsync(
        this IHost host, CancellationToken cancellationToken = default)
    {
        // The DbContext and the audit-log collection it depends on are scoped, so the
        // application's root provider cannot resolve them directly.
        await using var scope = host.Services.CreateAsyncScope();

        var logger = scope.ServiceProvider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger(typeof(DatabaseMigrationExtensions).FullName!);

        var db = scope.ServiceProvider.GetRequiredService<NibssNpsDbContext>();

        var pending = (await db.Database.GetPendingMigrationsAsync(cancellationToken)).ToList();

        if (pending.Count == 0)
        {
            logger.LogInformation("NIBSS NPS database schema is up to date; no migrations pending.");
            return host;
        }

        logger.LogInformation(
            "Applying {PendingCount} pending NIBSS NPS migration(s): {PendingMigrations}.",
            pending.Count,
            string.Join(", ", pending));

        await db.Database.MigrateAsync(cancellationToken);

        logger.LogInformation(
            "Applied {PendingCount} NIBSS NPS migration(s); schema is now up to date.",
            pending.Count);

        return host;
    }
}
