# Tollervey.Umbraco.LightningPayments

Lightning paywalls and tip jar for Umbraco, powered by Breez SDK (Liquid). Includes a backoffice dashboard, property editors, and website components.

Quick links
- Health: GET /health/ready
- Backoffice dashboard: Settings > Lightning Payments

Important defaults
- Storage: SQLite at App_Data/LightningPayments/payment.db (overridable via LightningPayments:ConnectionString).
- Migrations: Not required. The package creates/updates the schema at startup.
- HTTPS: Required by default (controllers/middleware).
- Swagger: Not bundled. Add Swashbuckle in your host if you want /swagger.

1) Quick start (NuGet consumer)
- Install the NuGet package into your Umbraco site.
- Configure secrets (local dev via User Secrets; production via environment variables):
  - LightningPayments:BreezApiKey
  - LightningPayments:Mnemonic
  - Optional: LightningPayments:ConnectionString (defaults to App_Data/LightningPayments/payment.db)

Example appsettings.json section (non-secrets only):
{
  "LightningPayments": {
    "ConnectionString": "Data Source=~/App_Data/LightningPayments/payment.db"
  }
}

- Start the site. Health should be green at /health/ready.

2) Drop-in UI usage
- Razor partials (auto-load the JS bundle; no layout edits needed):
  - @await Html.PartialAsync("LightningPayments/TipJar", Model)
  - @await Html.PartialAsync("LightningPayments/Paywall", Model)

- Or use components directly (then you must load the bundle once, e.g., in your layout):
  <script type="module" src="/App_Plugins/Tollervey.Umbraco.LightningPayments/lightning-ui.js"></script>

3) Website components
- Paywall
  <breez-paywall content-id="@Model.Id" title="Unlock content" description="One-time payment"></breez-paywall>
  Options:
  - pollIntervalMs (default 2000)
  - maxWaitMs (default 180000)

- Tip jar
  <breez-tip-jar title="Send a tip" description="Thanks!" showFiat fiat="USD"></breez-tip-jar>
  Options:
  - defaultAmount, amounts=[500,1000,2500] (sats)
  - contentId (optional)
  - showFiat, fiat (USD/EUR/GBP)

4) Data Types and property editors
- Lightning Paywall editor (alias: Tollervey.Breez.Paywall): stores Enabled and Fee (sats).
- Tip Jar editor (alias: Tollervey.Breez.TipJar): stores defaults for amounts/labels.
Steps:
1. Create Data Types for Paywall and Tip Jar in Settings > Data Types.
2. Add the properties to Document Types.
3. Configure values on content nodes.

5) Backoffice dashboard
- Settings > Lightning Payments shows connection status, limits, and an admin-only payments list.

6) LNURL and Bolt12 (optional)
- LNURL-Pay metadata: GET /api/public/lightning/GetLnurlPayInfo?contentId=123
- LNURL invoice callback: GET /api/public/lightning/GetLnurlInvoice?contentId=123&amount=100000 (msats)
- Bolt12 offer: GET /api/public/lightning/GetBolt12Offer?contentId=123

7) Configuration & security
- Secrets (API key, mnemonic, webhook secret) must not be stored in appsettings.json in Production; use environment variables or a secret store.
- Webhooks (optional) provide resilience for confirmations. If you set WebhookUrl, set LightningPayments:WebhookSecret and serve HTTPS.

8) Troubleshooting
- Health red: check Breez API key/mnemonic; inspect logs.
- Invoice never confirms: ensure the site is reachable (webhooks if configured) and check logs for the payment hash.
- 401 for management APIs: log into Umbraco backoffice in the same browser.
- SQLite write issues: ensure the App_Data path is writable.

Notes for contributors
- The backoffice/website bundle is built from Client and emitted as /App_Plugins/Tollervey.Umbraco.LightningPayments/lightning-ui.js. NuGet consumers do not need Node; the assets ship in the package.
