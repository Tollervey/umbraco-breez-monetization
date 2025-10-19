using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Microsoft.Extensions.Options;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Tollervey.LightningPayments.Breez.Configuration;
using Tollervey.LightningPayments.Breez.Services;

[TestClass]
public class SmtpEmailServiceTests
{
    [TestMethod]
    public async Task SendEmailAsync_SendsEmailCorrectly()
    {
        // Arrange
        var settings = new LightningPaymentsSettings
        {
            FromEmailAddress = "from@example.com",
            SmtpHost = "smtp.example.com",
            SmtpPort = 587,
            SmtpUsername = "user",
            SmtpPassword = "pass"
        };
        var optionsMock = new Mock<IOptions<LightningPaymentsSettings>>();
        optionsMock.Setup(o => o.Value).Returns(settings);

        var clientMock = new Mock<ISmtpClient>();
        clientMock.Setup(c => c.ConnectAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<SecureSocketOptions>(), default))
            .Returns(Task.CompletedTask);
        clientMock.Setup(c => c.AuthenticateAsync(It.IsAny<string>(), It.IsAny<string>(), default))
            .Returns(Task.CompletedTask);
        clientMock.Setup(c => c.SendAsync(It.IsAny<MimeMessage>(), default))
            .Returns(Task.FromResult("test-id"));
        clientMock.Setup(c => c.DisconnectAsync(true, default))
            .Returns(Task.CompletedTask);

        var service = new SmtpEmailService(optionsMock.Object, () => clientMock.Object);

        // Act
        await service.SendEmailAsync("to@example.com", "Test Subject", "Test Body");

        // Assert
        clientMock.Verify(c => c.ConnectAsync(settings.SmtpHost, settings.SmtpPort, SecureSocketOptions.StartTls, default), Times.Once());
        clientMock.Verify(c => c.AuthenticateAsync(settings.SmtpUsername, settings.SmtpPassword, default), Times.Once());
        clientMock.Verify(c => c.SendAsync(It.Is<MimeMessage>(m =>
            m.From[0].ToString() == settings.FromEmailAddress &&
            m.To[0].ToString() == "to@example.com" &&
            m.Subject == "Test Subject" &&
            ((TextPart)m.Body).Text == "Test Body"
        ), default), Times.Once());
        clientMock.Verify(c => c.DisconnectAsync(true, default), Times.Once());
    }
}
