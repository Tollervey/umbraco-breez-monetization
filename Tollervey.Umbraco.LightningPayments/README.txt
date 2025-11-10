      _       _              _
     | |     | |            | |
   __| | ___ | |_ _ __   ___| |_   _ __   _____      __
  / _` |/ _ \| __| '_ \ / _ \ __| | '_ \ / _ \ \ /\ / /
 | (_| | (_) | |_| | | |  __/ |_  | | | |  __/\ V  V /
  \__,_|\___/ \__|_| |_|\___|\__| |_| |_|\___| \_/\_/   _                 _

== Audience ==
- If you installed the NuGet package into an Umbraco site: you can skip Node. The website/backoffice assets are already included.
- If you are developing this package from source: read on.

== Requirements (for building from source only) ==
* Node LTS 20.17.0+
* Use NVM/Volta to manage Node versions

== Build steps (source) ==
* Open a terminal inside the \Client folder
* Run npm install
* Run npm run build
* Output goes to wwwroot\App_Plugins\Tollervey.Umbraco.LightningPayments\lightning-ui.js

== File Watching (source) ==
* Add this RCL as a project reference to a running Umbraco website
* From \Client: npm run watch to rebuild on changes

== Database configuration ==
Defaults:
- LightningPayments:ConnectionString defaults to "Data Source=~/App_Data/LightningPayments/payment.db".

Migrations:
- You do NOT need to run EF CLI migrations. The package creates/updates the SQLite schema automatically at startup (PaymentDbInitializer).
- For production, ensure the DB file is on persistent storage and backed up regularly.

== Configuration security ==
Use User Secrets (dev) and environment variables (prod). Do not store secrets in appsettings.json.

Local development (examples):
dotnet user-secrets set "LightningPayments:BreezApiKey" "your-api-key"
dotnet user-secrets set "LightningPayments:Mnemonic" "your-24-words"

Production (examples):
LightningPayments__BreezApiKey=your-api-key
LightningPayments__Mnemonic=your-24-words

== Rate limiting ==
See appsettings under LightningPayments:RateLimiting to enable ASP.NET Core rate limiting for invoice creation.

== Notes ==
* HTTPS is required.
* Swagger is not bundled; add Swashbuckle in your host if you want /swagger.
* Offline mode is not exposed by default.
