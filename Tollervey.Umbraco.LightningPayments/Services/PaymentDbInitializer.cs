using System.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Polly;

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
 // Retry policy for transient DB initialization failures
 var retryPolicy = Policy
 .Handle<Exception>()
 .WaitAndRetryAsync(new[] { TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(5) },
 onRetry: (exception, timeSpan, retryCount, context) =>
 {
 _logger.LogWarning(exception, "Database initialization attempt {RetryCount} failed. Retrying in {Delay}s...", retryCount, timeSpan.TotalSeconds);
 });

 try
 {
 await retryPolicy.ExecuteAsync(async (ct) =>
 {
 _logger.LogInformation("Starting payment database initialization...");

 using var scope = _sp.CreateScope();
 var ctx = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();

 // Ensure database and tables are created (no-op if present)
 await ctx.Database.EnsureCreatedAsync(ct);

 // Only attempt lightweight migrations for SQLite provider
 var provider = ctx.Database.ProviderName ?? string.Empty;
 if (!provider.Contains("Sqlite", StringComparison.OrdinalIgnoreCase))
 {
 _logger.LogInformation("Database provider is not SQLite ('{Provider}'), skipping SQLite-specific schema adjustments.", provider);
 return;
 }

 _logger.LogInformation("Running SQLite schema adjustments (if required)...");

 await EnsureColumnAsync(ctx, "PaymentStates", "AmountSat", "INTEGER NOT NULL DEFAULT0", ct);
 await EnsureColumnAsync(ctx, "PaymentStates", "Kind", "INTEGER NOT NULL DEFAULT0", ct);

 _logger.LogInformation("Payment database initialization completed successfully.");
 }, cancellationToken);
 }
 catch (OperationCanceledException)
 {
 _logger.LogWarning("Payment DB initialization was canceled.");
 throw;
 }
 catch (Exception ex)
 {
 _logger.LogError(ex, "Payment DB initialization failed after retries.");
 // Do not rethrow - allow application to continue but note that some features may not work.
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
 // Log that a schema change was applied
 // Note: callers should be conservative with schema changes in production environments.
 }
 }
 }
 }
}
