using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;

namespace Tollervey.Umbraco.LightningPayments.UI.Components;

public class PackageDiagnosticsComponent : IComponent
{
    private readonly ILogger<PackageDiagnosticsComponent> _logger;
    private readonly IWebHostEnvironment _env;

    private const string PluginDir = "App_Plugins/Tollervey.Umbraco.LightningPayments";
    private static readonly string[] ProbeFiles =
    [
        $"{PluginDir}/umbraco-package.json",
        $"{PluginDir}/lightning-ui.js"
    ];

    public PackageDiagnosticsComponent(ILogger<PackageDiagnosticsComponent> logger, IWebHostEnvironment env)
    {
        _logger = logger;
        _env = env;
    }

    public void Initialize()
    {
        try
        {
            var fp = _env.WebRootFileProvider;
            _logger.LogInformation("LightningPayments diagnostics: WebRootPath={WebRootPath}", _env.WebRootPath ?? "(null)");

            foreach (var relPath in ProbeFiles)
            {
                var fi = fp.GetFileInfo(relPath);
                _logger.LogInformation("Asset probe: {Path} Exists={Exists} Length={Length}",
                    "/" + relPath.Replace('\\','/'), fi.Exists, fi.Exists ? fi.Length : 0);
            }

            // List top-level files in the plugin directory (useful to verify hashed chunks are there)
            IDirectoryContents dir = fp.GetDirectoryContents(PluginDir);
            if (dir.Exists)
            {
                var files = dir.Where(f => !f.IsDirectory)
                               .Select(f => new { f.Name, f.Length })
                               .OrderBy(f => f.Name)
                               .Take(50) // keep logging concise
                               .ToArray();
                _logger.LogInformation("Asset listing for /{PluginDir} ({Count} items, showing up to 50): {Files}",
                    PluginDir, files.Length, string.Join(", ", files.Select(f => $"{f.Name}({f.Length}b)")));
            }
            else
            {
                _logger.LogWarning("Directory probe failed: /{PluginDir} not found via WebRootFileProvider.", PluginDir);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "LightningPayments diagnostics failed during startup probe.");
        }
    }

    public void Terminate()
    {
        // no-op
    }
}