using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Infrastructure.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Infrastructure.Services;

public sealed class DatabaseMigrationService : IDatabaseMigrationService
{
  private readonly IDbContextFactory<AppDbContext> _contextFactory;

  public DatabaseMigrationService(IDbContextFactory<AppDbContext> contextFactory)
  {
    _contextFactory = contextFactory;
  }

  public async Task<IReadOnlyList<string>> GetPendingMigrationsAsync(CancellationToken cancellationToken = default)
  {
    await using var db = await _contextFactory.CreateDbContextAsync(cancellationToken);
    return (await db.Database.GetPendingMigrationsAsync(cancellationToken)).ToList();
  }

  public async Task<IReadOnlyList<string>> ApplyPendingMigrationsAsync(CancellationToken cancellationToken = default)
  {
    await using var db = await _contextFactory.CreateDbContextAsync(cancellationToken);
    var pending = (await db.Database.GetPendingMigrationsAsync(cancellationToken)).ToList();

    try
    {
      // Always invoke MigrateAsync — it is idempotent and applies any newly discovered migrations.
      await db.Database.MigrateAsync(cancellationToken);
    }
    catch (InvalidOperationException ex) when (
        ex.Message.Contains("PendingModelChangesWarning", StringComparison.OrdinalIgnoreCase) ||
        ex.Message.Contains("pending changes", StringComparison.OrdinalIgnoreCase))
    {
      System.Diagnostics.Debug.WriteLine($"[AccountingMigrate] PendingModelChanges ignored: {ex.Message}");
    }
    catch (Exception ex) when (IsCreateDatabasePermissionDenied(ex))
    {
      var catalog = TryGetInitialCatalog(db);
      throw new InvalidOperationException(
          "تعذر إنشاء قاعدة البيانات على السيرفر (لا توجد صلاحية CREATE DATABASE).\n\n" +
          "تأكد أن قيمة Database= في سلسلة الاتصال تشير إلى قاعدة موجودة مسبقاً " +
          "وأن حساب SQL يملك صلاحية استخدامها" +
          (string.IsNullOrWhiteSpace(catalog) ? "." : $" (الحالي: {catalog}).") +
          "\nلا حاجة لإعادة تسمية القاعدة إن كان الاسم مضمّناً في الكونكشن سترنغ.",
          ex);
    }

    await AccountingSchemaRepair.ApplyAsync(db, cancellationToken);
    if (!await AccountingSchemaRepair.IsVoucherSchemaReadyAsync(db, cancellationToken))
    {
      throw new InvalidOperationException(AccountingSchemaRepair.StandaloneSchemaOutdatedMessage);
    }

    return pending;
  }

  private static bool IsCreateDatabasePermissionDenied(Exception ex)
  {
    for (var current = ex; current is not null; current = current.InnerException)
    {
      if (current is SqlException sql && sql.Number == 262)
        return true;

      if (current.Message.Contains("CREATE DATABASE permission denied", StringComparison.OrdinalIgnoreCase))
        return true;
    }

    return false;
  }

  private static string? TryGetInitialCatalog(AppDbContext db)
  {
    try
    {
      var cs = db.Database.GetConnectionString();
      if (string.IsNullOrWhiteSpace(cs))
        return null;
      return new SqlConnectionStringBuilder(cs).InitialCatalog;
    }
    catch
    {
      return null;
    }
  }
}
