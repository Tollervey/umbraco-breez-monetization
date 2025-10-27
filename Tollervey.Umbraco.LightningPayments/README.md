# Tollervey.Umbraco.LightningPayments

Lightning paywalls and tip jar for Umbraco, powered by Breez SDK (Liquid). Includes a backoffice dashboard, property editors, and website components.

Quick links
- Health: GET /health/ready
- Public API (dev swagger): /swagger
- Backoffice dashboard: Settings > Lightning Payments

1) Install and configure
- Add the package to your Umbraco site (NuGet or project reference).
- In appsettings.json add a LightningPayments section:

{
 "LightningPayments": {
 "ConnectionString": "Data Source=~/App_Data/payment.db",
 "BreezApiKey": "YOUR_API_KEY",
 "Mnemonic": "YOUR_24_WORDS",
 "ApplicationInsightsConnectionString": ""
 }
}

- Secure secrets via User Secrets or environment variables in production (see README.txt notes).
- Start the site. The SQLite schema is created automatically and lightweight migrations are applied at startup.

2) Health and connectivity
- Check /health/ready. When healthy, the Breez SDK is set up and the app is ready.
- In development, navigate to /swagger for public and management endpoints.

3) Data Types and property editors
This package registers two property editors under Data Types:
- Lightning Paywall (alias: Tollervey.Breez.Paywall)
 - Stores JSON with Enabled and Fee (sats)
- Tip Jar (alias: Tollervey.Breez.TipJar)
 - Stores JSON with enabled, defaultAmounts, label

Steps:
1. In Settings > Data Types, create a new Data Type using editor "Lightning Paywall" and save.
2. Create a "Tip Jar" Data Type (optional) and configure default amounts.
3. Add the Paywall/Tip Jar properties to your Document Types.
4. Set values on content nodes in the Content section.

4) Backoffice dashboard
- Settings > Lightning Payments shows connection status, health, limits, and admin actions.
- Admin-only "All payments" view lists records (requires Administrator).

5) Website components
The package ships Web Components you can place in templates or partials.

- Paywall component
<breez-paywall content-id="@Model.Id" title="Unlock content" description="One-time payment"></breez-paywall>

Behavior:
- Checks payment status for the current session/cookie and contentId.
- If unpaid, shows a button that opens an invoice modal and polls confirmation.
- On payment, reveals slotted content and emits a "breez-unlocked" event.

Options:
- pollIntervalMs (default2000)
- maxWaitMs (default180000)

- Tip jar component
<breez-tip-jar title="Send a tip" description="Thanks!" showFiat fiat="USD"></breez-tip-jar>

Behavior:
- Creates a tip invoice via public API and presents a modal with QR and BOLT11 string.
- Shows stats (count and total sats) from a read-only endpoint.

Options:
- defaultAmount, amounts=[500,1000,2500] (sats)
- contentId (optional)
- showFiat, fiat (USD/EUR/GBP)

6) Razor partials and macros
Use the provided partials or copy the patterns into your templates:

- Partial: Views/Partials/LightningPayments/Paywall.cshtml
@* Minimal example *@
<div>
 <breez-paywall content-id="@Model.Id"
 title="Unlock content"
 description="One-time payment to access this article"></breez-paywall>
 <div>
 @* Your gated content can be placed here or in a separate block revealed after payment *@
 </div>
</div>

- Partial: Views/Partials/LightningPayments/TipJar.cshtml
@* Minimal example *@
<div>
 <breez-tip-jar title="Send a tip" description="Thank you for supporting!" showFiat fiat="USD"></breez-tip-jar>
</div>

You can also wrap these in macros and drop them into rich editors if needed.

7) LNURL and Bolt12
- LNURL-Pay metadata: GET /api/public/lightning/GetLnurlPayInfo?contentId=123
- LNURL invoice callback: GET /api/public/lightning/GetLnurlInvoice?contentId=123&amount=100000 (msats)
- Optional Bolt12 offer: GET /api/public/lightning/GetBolt12Offer?contentId=123

8) Error handling and security
- All public and management endpoints return consistent JSON error payloads: { "error", "message" }.
- Backoffice management endpoints require Administrator access.
- HTTPS is required by default.
- Session cookie (for paywall state) is HttpOnly, Secure, SameSite=Strict.

9) Troubleshooting
- /health/ready fails: verify Breez keys and configuration; inspect logs.
- Invoices but no confirmation: ensure webhook events reach your site; check logs for payment hash/state.
- Admin endpoints return401 from Swagger: log into Umbraco backoffice in the same browser (same origin) or supply auth cookie.
- SQLite file write issues: verify file permissions on the configured path.
- Offline mode: You can register offline runtime mode in a custom composer if you want synthetic invoices for development/testing.

10) Packaging notes
- The backoffice bundle is declared in Client/public/umbraco-package.json and built to /App_Plugins/Tollervey.Umbraco.LightningPayments/lightning-ui.js.
- Property editor aliases: Tollervey.Breez.Paywall and Tollervey.Breez.TipJar.
- Dashboard: Lightning Payments Dashboard.

License and contributions
- Issues and PRs welcome.
