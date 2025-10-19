using Microsoft.VisualStudio.TestTools.UnitTesting;
using Tollervey.LightningPayments.Breez.Configuration;

[TestClass]
public class LightningPaymentsSettingsTests
{
    [TestMethod]
    public void Properties_CanBeSetAndGet()
    {
        var settings = new LightningPaymentsSettings
        {
            BreezApiKey = "key",
            Mnemonic = "mnemonic",
            WebhookUrl = "https://example.com/webhook",
            ConnectionString = "conn",
            PaywallUrl = "/pay",
            WebhookSecret = "secret",
            ApplicationInsightsConnectionString = "ai",
            AdminEmail = "admin@example.com",
            Network = LightningPaymentsSettings.LightningNetwork.Testnet,
            SmtpHost = "host",
            SmtpPort = 587,
            SmtpUsername = "user",
            SmtpPassword = "pass",
            FromEmailAddress = "from@example.com"
        };

        Assert.AreEqual("key", settings.BreezApiKey);
        Assert.AreEqual("mnemonic", settings.Mnemonic);
        Assert.AreEqual("https://example.com/webhook", settings.WebhookUrl);
        Assert.AreEqual("conn", settings.ConnectionString);
        Assert.AreEqual("/pay", settings.PaywallUrl);
        Assert.AreEqual("secret", settings.WebhookSecret);
        Assert.AreEqual("ai", settings.ApplicationInsightsConnectionString);
        Assert.AreEqual("admin@example.com", settings.AdminEmail);
        Assert.AreEqual(LightningPaymentsSettings.LightningNetwork.Testnet, settings.Network);
        Assert.AreEqual("host", settings.SmtpHost);
        Assert.AreEqual(587, settings.SmtpPort);
        Assert.AreEqual("user", settings.SmtpUsername);
        Assert.AreEqual("pass", settings.SmtpPassword);
        Assert.AreEqual("from@example.com", settings.FromEmailAddress);
    }
}
