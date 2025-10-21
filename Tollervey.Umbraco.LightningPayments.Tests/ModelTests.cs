using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class ModelTests
{
    [TestMethod]
    public void BreezWebhookPayload_Serialization()
    {
        var payload = new BreezWebhookPayload
        {
            Type = "payment_received",
            Payment = new PaymentDetails { Id = "test_id" }
        };

        var json = JsonSerializer.Serialize(payload);
        var deserialized = JsonSerializer.Deserialize<BreezWebhookPayload>(json);

        Assert.AreEqual(payload.Type, deserialized.Type);
        Assert.AreEqual(payload.Payment.Id, deserialized.Payment.Id);
    }

    [TestMethod]
    public void PaymentDetails_Serialization()
    {
        var details = new PaymentDetails { Id = "test_id" };

        var json = JsonSerializer.Serialize(details);
        var deserialized = JsonSerializer.Deserialize<PaymentDetails>(json);

        Assert.AreEqual(details.Id, deserialized.Id);
    }

    [TestMethod]
    public void PaywallConfig_Serialization()
    {
        var config = new PaywallConfig { Enabled = true, Fee = 1000 };

        var json = JsonSerializer.Serialize(config);
        var deserialized = JsonSerializer.Deserialize<PaywallConfig>(json);

        Assert.AreEqual(config.Enabled, deserialized.Enabled);
        Assert.AreEqual(config.Fee, deserialized.Fee);
    }

    [TestMethod]
    public void PaywallViewModel_Properties()
    {
        var vm = new PaywallViewModel
        {
            ContentId = 42,
            PreviewContent = "preview",
            Fee = 500
        };

        Assert.AreEqual(42, vm.ContentId);
        Assert.AreEqual("preview", vm.PreviewContent);
        Assert.AreEqual((ulong)500, vm.Fee);
    }
}
