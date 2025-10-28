      _       _              _                                                          
     | |     | |            | |                                                         
   __| | ___ | |_ _ __   ___| |_   _ __   _____      __                                 
  / _` |/ _ \| __| '_ \ / _ \ __| | '_ \ / _ \ \ /\ / /                                 
 | (_| | (_) | |_| | | |  __/ |_  | | | |  __/\ V  V /                                  
  \__,_|\___/ \__|_| |_|\___|\__| |_| |_|\___| \_/\_/   _                 _             
                 | |                                   | |               (_)            
  _   _ _ __ ___ | |__  _ __ __ _  ___ ___     _____  _| |_ ___ _ __  ___ _  ___  _ __  
 | | | | '_ ` _ \| '_ \| '__/ _` |/ __/ _ \   / _ \ \/ / __/ _ \ '_ \/ __| |/ _ \| '_ \ 
 | |_| | | | | | | |_) | | | (_| | (_| (_) | |  __/>  <| ||  __/ | | \__ \ | (_) | | | |
  \__,_|_| |_| |_|_.__/|_|  \__,_|\___\___/   \___/_/\_\\__\___|_| |_|___/_|\___/|_| |_|
                                                                                        

== Requirements ==
* Node LTS Version 20.17.0+
* Use a tool such as NVM (Node Version Manager) for your OS to help manage multiple versions of Node

== Node Version Manager tools ==
* https://github.com/coreybutler/nvm-windows
* https://github.com/nvm-sh/nvm
* https://docs.volta.sh/guide/getting-started

== Steps ==
* Open a terminal inside the `\Client` folder
* Run `npm install` to install all the dependencies
* Run `npm run build` to build the project
* The build output is copied to `wwwroot\App_Plugins\Tollervey.Umbraco.LightningPayments\lightning-ui.js`

== File Watching ==
* Add this Razor Class Library Project as a project reference to an Umbraco Website project
* From the `\Client` folder run the command `npm run watch` this will monitor the changes to the *.ts files and rebuild the project
* With the Umbraco website project running the Razor Class Library Project will refresh the browser when the build is complete

== Suggestion ==
* Use VSCode as the editor of choice as it has good tooling support for TypeScript and it will recommend a VSCode Extension for good Lit WebComponent completions

== Other Resources ==
* Umbraco Docs - https://docs.umbraco.com/umbraco-cms/customizing/overview

== Database Configuration and Management ==
This package uses Entity Framework Core with SQLite by default for persistent storage of payment states. SQLite is a lightweight, file-based database that integrates seamlessly with Umbraco and ASP.NET Core. Below are instructions for configuring and managing the database, especially in production environments.

Default Configuration
- The database is configured via the ConnectionString setting in the "LightningPayments" section of your appsettings.json file.
- Default value: "Data Source=payment.db". This creates a file named payment.db in your application's content root directory.
- The package registers the PaymentDbContext with SQLite in LightningPaymentsComposer.cs.

Installation and Migrations
1. After installing the package, open a terminal in your project's root directory.
2. Create an initial migration (if not already done): dotnet ef migrations add InitialCreate --project Tollervey.Umbraco.LightningPayments.UI.csproj
3. Apply the migration to create/update the database schema: dotnet ef database update --project Tollervey.Umbraco.LightningPayments.UI.csproj
- Run dotnet ef database update after package updates to apply any new migrations.

Production Considerations
- File Hosting and Persistence: The SQLite file (e.g., payment.db) is stored on the server's file system. You are responsible for:
  - Ensuring the application process (e.g., IIS AppPool user) has read/write permissions to the file location.
  - Backing up the file regularly, as it contains critical payment data.
  - In cloud environments (e.g., Azure App Service), use persistent storage like Azure Files or mounted volumes, as local file systems may be ephemeral.
- Customizing the Connection String:
  - Edit appsettings.json to override the default:

"LightningPayments": {
      "ConnectionString": "Data Source=~/App_Data/payment.db"  // Example: Store in Umbraco's App_Data folder
    }

  - For in-memory (non-persistent, testing only): "Data Source=:memory:"
- Best Practices:
  - Test with the in-memory provider for development (via InMemoryPaymentStateService).
  - Monitor database performance and consider migrating to a managed DB for production-critical data.
  - If you encounter issues, ensure EF Core tools are installed globally: dotnet tool install --global dotnet-ef.

For more details, refer to the EF Core documentation: https://learn.microsoft.com/en-us/ef/core/providers/sqlite/ and Umbraco extension guidelines.

== Configuration Security ==

Storing sensitive information such as API keys, mnemonics, and passwords directly in appsettings.json is insecure because this file is often checked into source control, potentially exposing secrets to unauthorized users. It also poses risks in production environments where configuration files might be accessible.

Instead, follow these best practices for handling secrets:

Local Development
Use the .NET User Secrets tool to store secrets outside of your project. This keeps them secure and separate from your codebase.

Example CLI command to set a user secret:
dotnet user-secrets set "LightningPayments:BreezApiKey" "your-api-key"
dotnet user-secrets set "LightningPayments:Mnemonic" "your-mnemonic"
dotnet user-secrets set "LightningPayments:SmtpPassword" "your-smtp-password"

Production
In production environments, use environment variables to provide secrets securely. For cloud-based deployments, consider using managed secret services like Azure Key Vault to store and retrieve secrets dynamically.

Example environment variable format:
LightningPayments__BreezApiKey=your-api-key
LightningPayments__Mnemonic=your-mnemonic
LightningPayments__SmtpPassword=your-smtp-password

This approach ensures that sensitive data is not hardcoded or stored in version-controlled files.

== Rate limiting configuration ==
This package exposes configurable rate limiting under the `LightningPayments:RateLimiting` section. Defaults are disabled. To enable the ASP.NET Core built-in rate limiter for invoice-generation endpoints, set `Enabled` and `UseAspNetRateLimiter` to `true` and tune limits appropriately.

Example `appsettings.json` snippet:

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

Programmatic configuration (C#)

If you prefer to configure rate limiting in code (for example within `Program.cs` or your application composer), configure `RateLimitingOptions` before calling `AddLightningPayments()`:

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
