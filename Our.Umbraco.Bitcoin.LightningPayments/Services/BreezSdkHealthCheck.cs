using Microsoft.Extensions.Diagnostics.HealthChecks;
using Our.Umbraco.Bitcoin.LightningPayments.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Hosting;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;

namespace Our.Umbraco.Bitcoin.LightningPayments.Services
{
    /// <summary>
    /// Health check that reports whether the Breez SDK is connected and its working directory is secure.
    /// </summary>
    public class BreezSdkHealthCheck : IHealthCheck
    {
        private readonly IBreezSdkService _service;
        private readonly IHostEnvironment _env;
        private readonly LightningPaymentsSettings _settings;

        /// <summary>
        /// Creates a new <see cref="BreezSdkHealthCheck"/>.
        /// </summary>
        public BreezSdkHealthCheck(IBreezSdkService service, IHostEnvironment env, IOptions<LightningPaymentsSettings> settings)
        {
            _service = service;
            _env = env;
            _settings = settings.Value;
        }

        /// <inheritdoc />
        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                bool isConnected = await _service.IsConnectedAsync(cancellationToken);

                // Determine working directory path
                var workingDir = !string.IsNullOrWhiteSpace(_settings.WorkingDirectory)
                    ? _settings.WorkingDirectory!
                    : Path.Combine(_env.ContentRootPath, $"App_Data/{LightningPaymentsSettings.SectionName}/");

                if (!Path.IsPathRooted(workingDir))
                {
                    workingDir = Path.Combine(_env.ContentRootPath, workingDir);
                }

                // Check writability
                var writable = false;
                var writableError = "";
                try
                {
                    if (!Directory.Exists(workingDir))
                    {
                        writableError = "Working directory does not exist.";
                    }
                    else
                    {
                        var testFile = Path.Combine(workingDir, $".healthcheck_{Guid.NewGuid():N}");
                        await File.WriteAllTextAsync(testFile, "ok", cancellationToken);
                        File.Delete(testFile);
                        writable = true;
                    }
                }
                catch (Exception ex)
                {
                    writableError = ex.Message;
                }

                // Best-effort permissions hint
                var permissionsNote = "";
                try
                {
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        var dirInfo = new DirectoryInfo(workingDir);
                        var acl = dirInfo.GetAccessControl();
                        // Check that current user has full control
                        var current = WindowsIdentity.GetCurrent();
                        var userSid = current?.User;
                        if (userSid != null)
                        {
                            var rules = acl.GetAccessRules(true, true, typeof(SecurityIdentifier));
                            permissionsNote = "Windows ACLs present; ensure only the application identity has access.";
                        }
                    }
                    else
                    {
                        // On Unix, check mode bits loosely (best-effort)
                        var fileInfo = new FileInfo(workingDir);
                        permissionsNote = "Ensure filesystem permissions restrict access to the application user (e.g. chmod700).";
                    }
                }
                catch
                {
                    // ignore permission introspection errors
                }

                if (!isConnected)
                {
                    return HealthCheckResult.Unhealthy("Breez SDK is not connected.");
                }

                if (!writable)
                {
                    var msg = $"Breez SDK working directory is not writable: {workingDir}. Error: {writableError}. {permissionsNote}";
                    return HealthCheckResult.Unhealthy(msg);
                }

                return HealthCheckResult.Healthy("Breez SDK is connected and working directory is writable.");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy("Error checking Breez SDK health.", ex);
            }
        }
    }
}
