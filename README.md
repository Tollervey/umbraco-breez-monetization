# Tollervey.Umbraco.LightningPayments

Umbraco Lightning payments and paywall integration powered by Breez.

- Supports Umbraco16.x (net8) and .NET9 targets in future steps
- Provides API endpoints, middleware, SSE realtime updates, and client assets
- Health check endpoint defaults to /health/ready

Quick start
- Install the NuGet package
- Register in a composer: `builder.AddLightningPayments();`
- Configure `LightningPayments` section in appsettings

Offline mode
- `builder.UseLightningPaymentsOffline(options => { /* ... */ });`

Rate limiting configuration

This package exposes configurable rate limiting under the `LightningPayments:RateLimiting` configuration section. The defaults are conservative and disabled by default. To enable the ASP.NET Core built-in rate limiter for invoice-generation endpoints, set `Enabled` and `UseAspNetRateLimiter` to `true` and tune the limits as appropriate for your site.

Example `appsettings.json` snippet:

```json
{
 "LightningPayments": {
 "RateLimiting": {
 "Enabled": true,
 "UseAspNetRateLimiter": true,
 "PermitLimit":5,
 "WindowSeconds":60,
 "QueueLimit":0,
 "RejectionStatusCode":429,
 "PartitionByIp": true
 }
 }
}
```

Field notes:
- `Enabled`: Turn on rate limiting for this package.
- `UseAspNetRateLimiter`: When true the package registers an ASP.NET Core `AddRateLimiter` policy named `InvoiceGeneration` and the composer enables the middleware. When false an in-process `IRateLimiter` implementation remains available for programmatic checks.
- `PermitLimit` and `WindowSeconds`: Control the fixed-window rate (e.g.,5 requests per60s).
- `QueueLimit`: How many requests may queue while waiting for permits (0 disables queuing).
- `PartitionByIp`: If true partitioning uses the remote IP address; otherwise a single default partition is used.

Programmatic configuration (C#)

If you prefer to configure rate limiting in code (for example within `Program.cs` or an application composer), you can configure `RateLimitingOptions` before calling `AddLightningPayments()`.

```csharp
// In Program.cs or your Umbraco composer registration
var rateLimiting = new Tollervey.Umbraco.LightningPayments.UI.Configuration.RateLimitingOptions
{
 Enabled = true,
 UseAspNetRateLimiter = true,
 PermitLimit =10,
 WindowSeconds =60,
 QueueLimit =0,
 RejectionStatusCode =429,
 PartitionByIp = true
};

// register the configured options
builder.Services.Configure<Tollervey.Umbraco.LightningPayments.UI.Configuration.RateLimitingOptions>(opts =>
{
 opts.Enabled = rateLimiting.Enabled;
 opts.UseAspNetRateLimiter = rateLimiting.UseAspNetRateLimiter;
 opts.PermitLimit = rateLimiting.PermitLimit;
 opts.WindowSeconds = rateLimiting.WindowSeconds;
 opts.QueueLimit = rateLimiting.QueueLimit;
 opts.RejectionStatusCode = rateLimiting.RejectionStatusCode;
 opts.PartitionByIp = rateLimiting.PartitionByIp;
});

// Then register the package
builder.AddLightningPayments();

// If using ASP.NET Rate Limiter, ensure middleware is enabled in your pipeline (composer attempts to enable it):
// app.UseRateLimiter();
```

Secrets & production enforcement

To reduce the risk of accidentally checking secrets into source control, the library enforces a fail-fast check in Production. If secret-like settings are present in the loaded configuration (for example in `appsettings.json`) and the corresponding environment variables are not provided, startup will fail and a critical log explains which keys must be moved to a secure provider.

Keys checked by the validator (examples of environment variable names):
- `LightningPayments__BreezApiKey`
- `LightningPayments__Mnemonic`
- `LightningPayments__WebhookSecret`
- `LightningPayments__SmtpPassword`

Recommended ways to supply secrets
- Development: use the .NET User Secrets store
 - Set via CLI: `dotnet user-secrets set "LightningPayments:BreezApiKey" "your-api-key"`
- Environment variables: provide values using the double-underscore convention (e.g., `LightningPayments__BreezApiKey`).
- Cloud/Production: use a managed secret store (recommended)
 - Azure Key Vault with the Key Vault configuration provider and managed identity
 - AWS Secrets Manager or similar secret stores
 - Configure the secret provider before `AddLightningPayments()` so values are present during options validation

If you need to allow a non-standard secret provider or opt out of the enforcement for a specific environment, contact the project maintainer or add a small wrapper in your host app to configure `RateLimitingOptions`/settings as required before calling `AddLightningPayments()` (not recommended for general use).

Troubleshooting
- If enabling ASP.NET rate limiting, ensure your host application pipeline still calls `app.UseRateLimiter()` (the package composer attempts to enable it in the Umbraco pipeline).

Troubleshooting
- Ensure database connection string is valid
- Check health at /health/ready

Repository: https://github.com/Tollervey/umbraco-breez-monetization
