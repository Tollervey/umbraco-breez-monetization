using System.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Tollervey.Umbraco.LightningPayments.UI.Services
{
 /// <summary>
 /// Lightweight initializer that ensures the PaymentStates table exists and
 /// applies simple schema upgrades for new columns when running on SQLite.
 /// </summary>
 internal sealed class PaymentDbInitializer : IHostedService
 {
 private readonly IServiceProvider _sp;
 private readonly ILogger<PaymentDbInitializer> _logger;

 public PaymentDbInitializer(IServiceProvider sp, ILogger<PaymentDbInitializer> logger)
 {
 _sp = sp;
 _logger = logger;
 }

 public async Task StartAsync(CancellationToken cancellationToken)
 {
 try
 {
 using var scope = _sp.CreateScope();
 var ctx = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();

 // Ensure database and tables are created (no-op if present)
 await ctx.Database.EnsureCreatedAsync(cancellationToken);

 // Only attempt lightweight migrations for SQLite provider
 var provider = ctx.Database.ProviderName ?? string.Empty;
 if (!provider.Contains("Sqlite", StringComparison.OrdinalIgnoreCase))
 {
 return; // external providers should use proper EF migrations
 }

 await EnsureColumnAsync(ctx, "PaymentStates", "AmountSat", "INTEGER NOT NULL DEFAULT 0", cancellationToken);
 await EnsureColumnAsync(ctx, "PaymentStates", "Kind", "INTEGER NOT NULL DEFAULT 0", cancellationToken);
 }
 catch (Exception ex)
 {
 _logger.LogError(ex, "Payment DB initialization failed.");
 }
 }

 public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

 private static async Task EnsureColumnAsync(PaymentDbContext ctx, string table, string column, string columnDefinition, CancellationToken ct)
 {
 await using var conn = (SqliteConnection)ctx.Database.GetDbConnection();
 if (conn.State != ConnectionState.Open) await conn.OpenAsync(ct);

 await using (var cmd = conn.CreateCommand())
 {
 cmd.CommandText = $"PRAGMA table_info('{table}')";
 var hasColumn = false;
 await using var reader = await cmd.ExecuteReaderAsync(ct);
 while (await reader.ReadAsync(ct))
 {
 var name = reader.GetString(1); // cid | name | type | notnull | dflt_value | pk
 if (string.Equals(name, column, StringComparison.OrdinalIgnoreCase))
 {
 hasColumn = true;
 break;
 }
 }

 if (!hasColumn)
 {
 await using var alter = conn.CreateCommand();
 alter.CommandText = $"ALTER TABLE {table} ADD COLUMN {column} {columnDefinition}";
 await alter.ExecuteNonQueryAsync(ct);
 }
 }
 }
 }
}
