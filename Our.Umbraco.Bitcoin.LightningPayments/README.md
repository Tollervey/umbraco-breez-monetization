# Our.Umbraco.Bitcoin.LightningPayments

A package to extend Umbraco 16 to allow nodeless Lightning Network payments with the BreezSdk. This package enables seamless integration of Bitcoin Lightning payments into Umbraco sites, including paywall functionality, real-time payment status updates via SSE, and webhook handling for payment confirmations.

## Features

- **Lightning Payments Integration**: Supports nodeless Lightning Network payments using Breez SDK.
- **Paywall Middleware**: Protects content behind a paywall with configurable payment requirements.
- **Real-Time Updates**: Server-Sent Events (SSE) for live payment status updates in the UI.
- **Webhook Support**: Handles Breez webhooks for payment confirmations and state changes.
- **Offline/Online Modes**: Configurable runtime modes for different deployment scenarios.
- **API Endpoints**: RESTful APIs for payment initiation, status checking, and management.
- **Umbraco Backoffice Integration**: Dashboard for monitoring payments and configuring settings.
- **Rate Limiting**: Built-in rate limiting to prevent abuse.
- **Diagnostics**: Health checks and logging for troubleshooting.

## Installation

1. Install the package via NuGet:
```
   dotnet add package Our.Umbraco.Bitcoin.LightningPayments
```
2. Configure the package in your `appsettings.json`:
```
   {
     "LightningPayments": {
       "RuntimeMode": "Online", // or "Offline"
       "BreezApiKey": "your-breez-api-key",
       "WebhookSecret": "your-webhook-secret",
       "RateLimiting": {
         "Enabled": true,
         "MaxRequestsPerMinute": 10
       }
     }
   }
```
3. Run the database migrations to create necessary tables.

4. Access the Lightning Payments section in the Umbraco backoffice to configure and monitor payments.

## Usage

### Protecting Content with Paywall

Use the paywall middleware by adding the `[Paywall]` attribute or configuring routes in your startup.

### Initiating Payments

Use the API endpoints to create payment requests:

```
// Example: Create a payment request
var paymentRequest = await _lightningPaymentsApi.CreatePaymentAsync(new PaymentRequest
{
    Amount = 1000, // in sats
    Description = "Access to premium content"
});
```

### Handling Webhooks

The package automatically handles Breez webhooks at `/umbraco/api/breez/webhook`.

## Configuration

- **RuntimeMode**: Set to "Online" for live payments or "Offline" for testing/demo mode.
- **BreezApiKey**: Your Breez API key for authentication.
- **WebhookSecret**: Secret for validating incoming webhooks.
- **RateLimiting**: Configure request limits to prevent abuse.

## Development

### Building Client Assets

The package includes client-side assets built with Vite. To build:

```
cd Client
npm install
npm run build
```

### Testing

Run the included unit tests and integration tests.

## Contributing

Contributions are welcome! Please submit issues and pull requests to the [GitHub repository](https://github.com/Tollervey/umbraco-breez-monetization).

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Changelog

See [CHANGELOG.md](CHANGELOG.md) for version history.