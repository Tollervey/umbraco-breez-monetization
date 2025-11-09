using System.IO;
using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Configuration.UserSecrets;
using Our.Umbraco.Bitcoin.LightningPayments.Configuration;
using Our.Umbraco.Bitcoin.LightningPayments.Services;
using Umbraco.Cms.Api.Management.Controllers;
using Umbraco.Cms.Web.Common.Authorization;

namespace Our.Umbraco.Bitcoin.LightningPayments.Controllers;

[Authorize(Policy = AuthorizationPolicies.RequireAdminAccess)]
[Route("umbraco/management/api/[controller]")]
[ApiController]
public class LightningSetupController : ManagementApiControllerBase
{
    private readonly IHostEnvironment _env;
    private readonly LightningPaymentsSettings _settings;

    public LightningSetupController(IHostEnvironment env, IOptions<LightningPaymentsSettings> settings)
    {
        _env = env;
        _settings = settings.Value;
    }

    [HttpGet("State")]
    public IActionResult GetState()
    {
        var dev = _env.IsDevelopment();
        var hasApiKey = !string.IsNullOrWhiteSpace(_settings.BreezApiKey);
        var hasMnemonic = !string.IsNullOrWhiteSpace(_settings.Mnemonic);
        var conn = _settings.ConnectionString;
        var network = _settings.Network.ToString();

        return Ok(new
        {
            environment = dev ? "Development" : (_env.IsProduction() ? "Production" : "Other"),
            canSaveDevSecrets = dev,
            hasApiKey,
            hasMnemonic,
            connectionString = conn,
            network
        });
    }

    public sealed class SaveSecretsRequest
    {
        public string? BreezApiKey { get; set; }
        public string? Mnemonic { get; set; }
        public string? ConnectionString { get; set; }
        public string? Network { get; set; } // "Mainnet" | "Testnet" | "Regtest"
    }

    [HttpPost("SaveDevSecrets")]
    public IActionResult SaveDevSecrets([FromBody] SaveSecretsRequest req)
    {
        if (!_env.IsDevelopment())
        {
            return BadRequest(new { error = "not_allowed", message = "Saving secrets is only allowed in Development." });
        }
        if (req == null || string.IsNullOrWhiteSpace(req.BreezApiKey) || string.IsNullOrWhiteSpace(req.Mnemonic))
        {
            return BadRequest(new { error = "invalid_request", message = "BreezApiKey and Mnemonic are required." });
        }

        // Validate network value if provided (case-insensitive)
        string? normalizedNetwork = null;
        if (!string.IsNullOrWhiteSpace(req.Network))
        {
            if (!Enum.TryParse<LightningPaymentsSettings.LightningNetwork>(req.Network, ignoreCase: true, out var parsed))
            {
                return BadRequest(new { error = "invalid_request", message = "Network must be one of: Mainnet, Testnet, Regtest." });
            }
            normalizedNetwork = parsed.ToString();
        }

        var secretsId = GetUserSecretsId();
        if (string.IsNullOrWhiteSpace(secretsId))
        {
            return BadRequest(new { error = "missing_usersecretsid", message = "UserSecretsId is not available on this application. Run 'dotnet user-secrets init' in the website project." });
        }

        var path = GetSecretsPath(secretsId);
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var json = System.IO.File.Exists(path) ? System.IO.File.ReadAllText(path) : "{}";
            using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(json) ? "{}" : json);
            var root = doc.RootElement.Clone();

            // Merge helper
            var dict = root.ValueKind == JsonValueKind.Object
                ? root.EnumerateObject().ToDictionary(p => p.Name, p => p.Value.Clone())
                : new Dictionary<string, JsonElement>();

            // Ensure nested LightningPayments object
            var lp = dict.ContainsKey("LightningPayments") && dict["LightningPayments"].ValueKind == JsonValueKind.Object
                ? dict["LightningPayments"].EnumerateObject().ToDictionary(p => p.Name, p => p.Value.Clone())
                : new Dictionary<string, JsonElement>();

            lp["BreezApiKey"] = JsonDocument.Parse(JsonSerializer.Serialize(req.BreezApiKey.Trim())).RootElement.Clone();
            lp["Mnemonic"] = JsonDocument.Parse(JsonSerializer.Serialize(req.Mnemonic.Trim())).RootElement.Clone();
            if (!string.IsNullOrWhiteSpace(req.ConnectionString))
            {
                lp["ConnectionString"] = JsonDocument.Parse(JsonSerializer.Serialize(req.ConnectionString.Trim())).RootElement.Clone();
            }
            if (!string.IsNullOrWhiteSpace(normalizedNetwork))
            {
                lp["Network"] = JsonDocument.Parse(JsonSerializer.Serialize(normalizedNetwork)).RootElement.Clone();
            }

            dict["LightningPayments"] = JsonDocument.Parse(JsonSerializer.Serialize(lp)).RootElement.Clone();

            var finalObj = JsonDocument.Parse(JsonSerializer.Serialize(dict)).RootElement.Clone();
            var finalJson = JsonSerializer.Serialize(finalObj, new JsonSerializerOptions { WriteIndented = true });
            System.IO.File.WriteAllText(path, finalJson);

            // Inform caller to restart for SDK to pick up fresh values
            return Ok(new { status = "saved", secretsPath = path, restartRequired = true });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "write_failed", message = "Failed to write dev secrets file.", detail = ex.Message });
        }
    }

    [HttpGet("Runtime")]
    public async Task<IActionResult> GetRuntime([FromServices] IRuntimeSettingsService runtime, CancellationToken ct)
    {
        var flags = await runtime.GetAsync(ct);
        return Ok(flags);
    }

    [HttpPost("Runtime")]
    public async Task<IActionResult> SaveRuntime([FromServices] IRuntimeSettingsService runtime, [FromBody] RuntimeFeatureFlags flags, CancellationToken ct)
    {
        if (flags is null) return BadRequest(new { error = "invalid_request", message = "Flags payload is required." });
        await runtime.SaveAsync(flags, ct);
        return Ok(new { status = "saved" });
    }

    private static string? GetUserSecretsId()
    {
        var asm = Assembly.GetEntryAssembly();
        var attr = asm?.GetCustomAttribute<UserSecretsIdAttribute>();
        return attr?.UserSecretsId;
    }

    private static string GetSecretsPath(string id)
    {
        if (OperatingSystem.IsWindows())
        {
            var baseDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return Path.Combine(baseDir, "Microsoft", "UserSecrets", id, "secrets.json");
        }
        else
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return Path.Combine(home, ".microsoft", "usersecrets", id, "secrets.json");
        }
    }
}
