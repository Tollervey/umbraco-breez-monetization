using System.Text.RegularExpressions;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Our.Umbraco.Bitcoin.LightningPayments.Infrastructure
{
    internal static class ConnectionStringResolver
    {
        private static readonly Regex DataDirToken = new(@"\|DataDirectory\|", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static string Resolve(string connStr, IHostEnvironment env, ILogger? logger = null)
        {
            if (string.IsNullOrWhiteSpace(connStr))
                throw new ArgumentException("LightningPayments connection string is missing.", nameof(connStr));

            // Replace |DataDirectory| with "<ContentRoot>/umbraco/Data"
            var dataDir = Path.Combine(env.ContentRootPath, "umbraco", "Data");
            Directory.CreateDirectory(dataDir);
            var replaced = DataDirToken.Replace(connStr, dataDir);

            // Parse as SQLite and normalize path
            var builder = new SqliteConnectionStringBuilder(replaced);

            if (!string.IsNullOrWhiteSpace(builder.DataSource))
            {
                // If relative path, make absolute under ContentRootPath
                if (!Path.IsPathRooted(builder.DataSource) && !builder.DataSource.Equals(":memory:", StringComparison.OrdinalIgnoreCase))
                {
                    builder.DataSource = Path.GetFullPath(Path.Combine(env.ContentRootPath, builder.DataSource));
                }

                // Ensure DB directory exists
                var dbDir = Path.GetDirectoryName(builder.DataSource);
                if (!string.IsNullOrWhiteSpace(dbDir))
                {
                    Directory.CreateDirectory(dbDir);
                }
            }

            // Sensible defaults
            if (!builder.TryGetValue("Cache", out _))
            {
                builder.Cache = SqliteCacheMode.Shared;
            }

            builder.ForeignKeys = true;
            builder.Pooling = true;

            logger?.LogDebug("LightningPayments SQLite DataSource resolved to: {DataSource}", builder.DataSource);
            return builder.ToString();
        }
    }
}
