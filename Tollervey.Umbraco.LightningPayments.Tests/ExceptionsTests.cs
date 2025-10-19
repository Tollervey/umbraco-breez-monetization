using Microsoft.VisualStudio.TestTools.UnitTesting;
using Tollervey.LightningPayments.Breez.Models;
using System;

[TestClass]
public class ExceptionsTests
{
    [TestMethod]
    public void PaymentException_DefaultConstructor()
    {
        var ex = new PaymentException();
        Assert.IsNotNull(ex);
        Assert.IsNull(ex.InnerException);
    }

    [TestMethod]
    public void PaymentException_WithMessage()
    {
        string message = "Test message";
        var ex = new PaymentException(message);
        Assert.AreEqual(message, ex.Message);
        Assert.IsNull(ex.InnerException);
    }

    [TestMethod]
    public void PaymentException_WithMessageAndInner()
    {
        string message = "Test message";
        var inner = new Exception("Inner");
        var ex = new PaymentException(message, inner);
        Assert.AreEqual(message, ex.Message);
        Assert.AreEqual(inner, ex.InnerException);
    }

    [TestMethod]
    public void WebhookException_DefaultConstructor()
    {
        var ex = new WebhookException();
        Assert.IsNotNull(ex);
        Assert.IsNull(ex.InnerException);
    }

    [TestMethod]
    public void WebhookException_WithMessage()
    {
        string message = "Test message";
        var ex = new WebhookException(message);
        Assert.AreEqual(message, ex.Message);
        Assert.IsNull(ex.InnerException);
    }

    [TestMethod]
    public void WebhookException_WithMessageAndInner()
    {
        string message = "Test message";
        var inner = new Exception("Inner");
        var ex = new WebhookException(message, inner);
        Assert.AreEqual(message, ex.Message);
        Assert.AreEqual(inner, ex.InnerException);
    }

    [TestMethod]
    public void InvoiceException_DefaultConstructor()
    {
        var ex = new InvoiceException();
        Assert.IsNotNull(ex);
        Assert.IsNull(ex.InnerException);
    }

    [TestMethod]
    public void InvoiceException_WithMessage()
    {
        string message = "Test message";
        var ex = new InvoiceException(message);
        Assert.AreEqual(message, ex.Message);
        Assert.IsNull(ex.InnerException);
    }

    [TestMethod]
    public void InvoiceException_WithMessageAndInner()
    {
        string message = "Test message";
        var inner = new Exception("Inner");
        var ex = new InvoiceException(message, inner);
        Assert.AreEqual(message, ex.Message);
        Assert.AreEqual(inner, ex.InnerException);
    }
}
