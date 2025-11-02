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

    private const string PackageFolderName = "Tollervey.Umbraco.LightningPayments";
    private const string PluginDirRelative = $"App_Plugins/{PackageFolderName}";
    private static readonly string[] ProbeFiles = ["umbraco-package.json", "lightning-ui.js"];

    public PackageDiagnosticsComponent(ILogger<PackageDiagnosticsComponent> logger, IWebHostEnvironment env)
    {
        _logger = logger;
        _env = env;
    }

    public void Initialize()
    {
        try
        {
            _logger.LogInformation("LightningPayments diagnostics: WebRootPath={WebRootPath}", _env.WebRootPath ?? "(null)");
            var fp = _env.WebRootFileProvider;

            // Probe physical /App_Plugins (when assets copied to site)
            ProbeLocation(fp, PluginDirRelative, "site-wwwroot");

            // Probe RCL static web assets path /_content/{Assembly}/App_Plugins/...
            var assemblyName = typeof(PackageDiagnosticsComponent).Assembly.GetName().Name!;
            var rclBase = $"_content/{assemblyName}/{PluginDirRelative}";
            ProbeLocation(fp, rclBase, "static-web-assets");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "LightningPayments diagnostics failed during startup probe.");
        }
    }

    private void ProbeLocation(IFileProvider fp, string basePath, string label)
    {
        foreach (var f in ProbeFiles)
        {
            var path = $"{basePath}/{f}".Replace('\\', '/');
            var fi = fp.GetFileInfo(path);
            _logger.LogInformation("Asset probe ({Label}): /{Path} Exists={Exists} Length={Length}",
                label, path, fi.Exists, fi.Exists ? fi.Length : 0);
        }

        IDirectoryContents dir = fp.GetDirectoryContents(basePath);
        if (dir.Exists)
        {
            var files = dir.Where(i => !i.IsDirectory)
                           .Select(i => $"{i.Name}({i.Length}b)")
                           .OrderBy(n => n)
                           .ToArray();
            _logger.LogInformation("Asset listing ({Label}) for /{BasePath} ({Count} items, showing up to 50): {Files}",
                label, basePath, files.Length, string.Join(", ", files.Take(50)));
        }
        else
        {
            _logger.LogWarning("Directory probe ({Label}) failed: /{BasePath} not found via WebRootFileProvider.",
                label, basePath);
        }
    }

    public void Terminate() { }
}