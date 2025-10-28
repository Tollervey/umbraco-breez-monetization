Sample usage

This repository ships a NuGet package intended for Umbraco. For a minimal sample host:

1. Create a new empty ASP.NET Core project targeting net8.0.
2. Install the NuGet package produced from this repo.
3. In Program.cs or a Composer, call `builder.AddLightningPayments();` and optionally `builder.AddLightningPaymentsApplicationInsightsFromConfig();`.
4. Configure `LightningPayments` section in appsettings.json with Breez keys and SMTP settings.

Note: A full sample app is recommended but omitted from this repo to keep the package focused.