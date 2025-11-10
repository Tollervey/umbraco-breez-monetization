using System.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Tollervey.Umbraco.LightningPayments.UI.Configuration;

namespace Tollervey.Umbraco.LightningPayments.UI.Services
{
    /// <summary>
    /// Lightweight initializer that ensures the PaymentStates table exists and
    /// applies simple schema upgrades for new columns when running on SQLite.
    /// Also ensures IdempotencyMappings table exists for idempotency support.
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
            // If offline and using in-memory state, skip DB initialization entirely.
            using (var preScope = _sp.CreateScope())
            {
                var runtimeMode = preScope.ServiceProvider.GetService<ILightningPaymentsRuntimeMode>();
                var offlineOpts = preScope.ServiceProvider.GetService<IOptions<OfflineLightningPaymentsOptions>>()?.Value;

                if (runtimeMode?.IsOffline == true && offlineOpts?.UseInMemoryStateService == true)
                {
                    _logger.LogInformation("LightningPayments running in OFFLINE mode (in-memory). Skipping SQLite initialization.");
                    return;
                }
            }

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
                    var env = scope.ServiceProvider.GetService<IHostEnvironment>();

                    // Ensure database and tables are created (no-op if present)
                    await ctx.Database.EnsureCreatedAsync(ct);

                    // Only attempt lightweight migrations for SQLite provider
                    var provider = ctx.Database.ProviderName ?? string.Empty;
                    if (!provider.Contains("Sqlite", StringComparison.OrdinalIgnoreCase))
                    {
                        _logger.LogInformation("Database provider is not SQLite ('{Provider}'), skipping SQLite-specific schema adjustments.", provider);

                        // If running in production and using a non-file-based provider (e.g., managed DB) we still recommend regular backups
                        if (env?.IsProduction() == true)
                        {
                            _logger.LogInformation("Running in Production with provider {Provider}. Ensure regular backups are configured for your managed database.", provider);
                        }

                        return;
                    }

                    // Log the resolved SQLite DataSource path to aid diagnostics
                    try
                    {
                        var connStr = ctx.Database.GetDbConnection().ConnectionString ?? string.Empty;
                        var b = new SqliteConnectionStringBuilder(connStr);
                        if (!string.IsNullOrWhiteSpace(b.DataSource))
                        {
                            _logger.LogDebug("LightningPayments SQLite DataSource path: {DataSource}", b.DataSource);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug(ex, "Failed to parse SQLite connection string for logging.");
                    }

                    _logger.LogInformation("Running SQLite schema adjustments (if required)...");

                    await EnsureColumnAsync(ctx, "PaymentStates", "AmountSat", "INTEGER NOT NULL DEFAULT0", ct);
                    await EnsureColumnAsync(ctx, "PaymentStates", "Kind", "INTEGER NOT NULL DEFAULT0", ct);

                    // Ensure IdempotencyMappings table exists. Create minimal table if missing.
                    await EnsureIdempotencyTableAsync(ctx, ct);

                    // If running in production and using a file-based SQLite DB, warn about backups and durability
                    try
                    {
                        var connStr = ctx.Database.GetDbConnection().ConnectionString ?? string.Empty;
                        var looksLikeFile = connStr.Contains("Data Source", StringComparison.OrdinalIgnoreCase) && !connStr.Contains(":memory:", StringComparison.OrdinalIgnoreCase);
                        if (env?.IsProduction() == true && looksLikeFile)
                        {
                            _logger.LogCritical("Payment database is configured to use a SQLite file in Production (ConnectionString: {Conn}). This is not recommended for production workloads.\n" +
                                                "Consider migrating to a managed database (e.g., Azure SQL, AWS RDS, or Azure Database for SQLite with persistent storage) and configure regular backups.\n" +
                                                "If you must use SQLite, ensure the file is placed on a persistent, backed-up volume and that the process has exclusive write access. Back up with 'sqlite3 <db> .dump > backup.sql' or copy the file while the application is stopped.", connStr);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to evaluate database connection string for backup recommendations.");
                    }

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

        private static async Task EnsureIdempotencyTableAsync(PaymentDbContext ctx, CancellationToken ct)
        {
            await using var conn = (SqliteConnection)ctx.Database.GetDbConnection();
            if (conn.State != ConnectionState.Open) await conn.OpenAsync(ct);

            // Check for table existence
            await using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='IdempotencyMappings'";
                var exists = false;
                await using var reader = await cmd.ExecuteReaderAsync(ct);
                if (await reader.ReadAsync(ct)) exists = true;

                if (!exists)
                {
                    // Create a minimal table suitable for our mapping
                    await using var create = conn.CreateCommand();
                    create.CommandText = @"CREATE TABLE IdempotencyMappings (
 IdempotencyKey TEXT PRIMARY KEY,
 PaymentHash TEXT NOT NULL,
 Invoice TEXT NOT NULL,
 CreatedAt TEXT NOT NULL,
 Status INTEGER NOT NULL DEFAULT0
 );";
                    await create.ExecuteNonQueryAsync(ct);
                }
            }
        }
    }
}
