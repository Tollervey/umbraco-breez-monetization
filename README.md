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

Troubleshooting
- Ensure database connection string is valid
- Check health at /health/ready

Repository: https://github.com/Tollervey/umbraco-breez-monetization
