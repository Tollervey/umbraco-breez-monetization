using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Our.Umbraco.Bitcoin.LightningPayments.Services;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;

namespace Our.Umbraco.Bitcoin.LightningPayments.Components;

public class PackageDiagnosticsComponent : IComponent
{
    private readonly ILogger<PackageDiagnosticsComponent> _logger;
    private readonly IWebHostEnvironment _env;
    private readonly IPaywallMessageService _paywallMessageService;

    private const string PackageFolderName = "Our.Umbraco.Bitcoin.LightningPayments";
    private const string PluginDirRelative = $"App_Plugins/{PackageFolderName}";
    private static readonly string[] ProbeFiles = ["umbraco-package.json", "lightning-ui.js"];

    public PackageDiagnosticsComponent(ILogger<PackageDiagnosticsComponent> logger, IWebHostEnvironment env, IPaywallMessageService paywallMessageService)
    {
        _logger = logger;
        _env = env;
        _paywallMessageService = paywallMessageService;
    }

    public void Initialize()
    {
        try
        {
            _logger.LogInformation("LightningPayments diagnostics: WebRootPath={WebRootPath}", _env.WebRootPath ?? "(null)");
            _logger.LogInformation("Paywall message: {Message}", _paywallMessageService.GetMessage());
            var fp = _env.WebRootFileProvider;

            // Probe physical /App_Plugins (when assets copied to site)
            ProbeLocation(fp, PluginDirRelative, "site-wwwroot");

            // Extra: inspect lightning-ui.js and its referenced bundle to confirm manifests present
            AnalyzeBundle(fp, PluginDirRelative);

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

    private void AnalyzeBundle(IFileProvider fp, string basePath)
    {
        var uiPath = $"{basePath}/lightning-ui.js";
        var uiFi = fp.GetFileInfo(uiPath);
        if (!uiFi.Exists)
        {
            _logger.LogWarning("Bundle analysis: {UiPath} not found.", uiPath);
            return;
        }

        string uiContent = ReadFirstBytes(uiFi, 2048);
        _logger.LogInformation("Bundle analysis: lightning-ui.js head: {Snippet}", Truncate(uiContent, 180));

        // Try extract referenced bundle file name from the small ES module wrapper
        // Example: import { a as e } from "./bundle.manifests-XYZ.js"; export { e as manifests };
        var m = Regex.Match(uiContent ?? string.Empty, "bundle\\.manifests(?:-[A-Za-z0-9_]+)?\\.js");
        if (!m.Success)
        {
            _logger.LogWarning("Bundle analysis: could not locate referenced bundle.manifests-*.js in lightning-ui.js");
            return;
        }
        var bundleFile = m.Value;
        var bundlePath = $"{basePath}/{bundleFile}";
        var bundleFi = fp.GetFileInfo(bundlePath);
        _logger.LogInformation("Bundle analysis: lightning-ui.js references {Bundle} Exists={Exists} Length={Length}",
            bundleFile, bundleFi.Exists, bundleFi.Exists ? bundleFi.Length : 0);

        if (!bundleFi.Exists)
        {
            _logger.LogWarning("Bundle analysis: referenced bundle {Bundle} not found next to lightning-ui.js", bundleFile);
            return;
        }

        string bundleHead = ReadFirstBytes(bundleFi, 32768); // read head chunk; enough to include manifest arrays
        bool hasSection = bundleHead.Contains("type:\"section\"") || bundleHead.Contains("type:\"section\"".Replace("\\\"", "\""));
        bool hasWorkspace = bundleHead.Contains("type:\"workspace\"");
        bool mentionsSectionAlias = bundleHead.Contains("Our.Bitcoin.LightningPayments.Section");
        bool mentionsSettings = bundleHead.Contains("value:\"settings\"");

        _logger.LogInformation("Bundle analysis summary: SectionManifest={HasSection} WorkspaceManifest={HasWorkspace} MentionsSectionAlias={MentionsSectionAlias} MentionsSettingsCondition={MentionsSettings}",
            hasSection, hasWorkspace, mentionsSectionAlias, mentionsSettings);

        // Log which dashboard condition was found
        if (mentionsSectionAlias)
        {
            _logger.LogInformation("Bundle analysis: Dashboard appears to target custom section alias 'Our.Bitcoin.LightningPayments.Section'.");
        }
        else if (mentionsSettings)
        {
            _logger.LogInformation("Bundle analysis: Dashboard appears to target built-in 'settings' section.");
        }
        else
        {
            _logger.LogWarning("Bundle analysis: Could not find dashboard SectionAlias condition in bundle head chunk.");
        }

        // Added: Detailed manifest detection logging
        _logger.LogInformation("Bundle analysis detail: Raw head snippet (first 500 chars): {Snippet}", Truncate(bundleHead, 500));
        _logger.LogInformation("Bundle analysis detail: Detected custom section: {Detected}", bundleHead.Contains("\"alias\": \"Our.Bitcoin.LightningPayments.Section\""));
        _logger.LogInformation("Bundle analysis detail: Detected workspace: {Detected}", bundleHead.Contains("\"alias\": \"Our.Bitcoin.LightningPayments.Workspace\""));
        _logger.LogInformation("Bundle analysis detail: Detected workspace view: {Detected}", bundleHead.Contains("\"alias\": \"Our.Bitcoin.LightningPayments.WorkspaceView.Main\""));
        _logger.LogInformation("Bundle analysis detail: Detected dashboard: {Detected}", bundleHead.Contains("\"alias\": \"Our.Bitcoin.LightningPayments.Dashboard\""));
        _logger.LogInformation("Bundle analysis detail: Custom element reference: {Detected}", bundleHead.Contains("elementName: 'lightning-payments-dashboard'"));
        _logger.LogInformation("Bundle analysis detail: Loader import: {Detected}", bundleHead.Contains("import('./dashboard.element')"));
    }

    private static string ReadFirstBytes(IFileInfo fi, int maxBytes)
    {
        try
        {
            using var s = fi.CreateReadStream();
            var len = (int)Math.Min(maxBytes, fi.Length);
            var buf = new byte[len];
            var read = s.Read(buf, 0, len);
            return Encoding.UTF8.GetString(buf, 0, read);
        }
        catch
        {
            return string.Empty;
        }
    }

    private static string Truncate(string? value, int max)
        => string.IsNullOrEmpty(value) ? string.Empty : (value!.Length <= max ? value : value.Substring(0, max));

    public void Terminate() { }
}

   



