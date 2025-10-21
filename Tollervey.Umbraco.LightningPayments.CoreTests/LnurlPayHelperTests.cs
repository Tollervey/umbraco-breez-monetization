using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Tollervey.Umbraco.LightningPayments.UI.Models;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Extensions;

namespace Tollervey.Umbraco.LightningPayments.CoreTests;

[TestClass]
public class LnurlPayHelperTests
{
    private Mock<IUmbracoContextAccessor> _umbracoContextAccessorMock;
    private Mock<ILogger> _loggerMock;
    private Mock<HttpRequest> _requestMock;

    [TestInitialize]
    public void Setup()
    {
        _umbracoContextAccessorMock = new Mock<IUmbracoContextAccessor>();
        _loggerMock = new Mock<ILogger>();
        _requestMock = new Mock<HttpRequest>();
        _requestMock.Setup(r => r.Scheme).Returns("https");
        _requestMock.Setup(r => r.Host).Returns(new HostString("test.com"));
    }

    [TestMethod]
    public void GetLnurlPayInfo_InvalidContentId_ReturnsBadRequest()
    {
        var result = LnurlPayHelper.GetLnurlPayInfo(0, _umbracoContextAccessorMock.Object, _loggerMock.Object, _requestMock.Object, "/callback");

        Assert.IsInstanceOfType(result, typeof(BadRequestObjectResult));
        var badRequest = (BadRequestObjectResult)result;
        Assert.AreEqual("Invalid content ID.", badRequest.Value);
    }

    [TestMethod]
    public void GetLnurlPayInfo_ContentNotFound_ReturnsNotFound()
    {
        var umbracoContextMock = new Mock<IUmbracoContext>();
        umbracoContextMock.Setup(c => c.Content.GetById(1)).Returns((IPublishedContent)null);

        IUmbracoContext ctx = umbracoContextMock.Object;
        _umbracoContextAccessorMock.Setup(a => a.TryGetUmbracoContext(out ctx)).Returns(true);

        var result = LnurlPayHelper.GetLnurlPayInfo(1, _umbracoContextAccessorMock.Object, _loggerMock.Object, _requestMock.Object, "/callback");

        Assert.IsInstanceOfType(result, typeof(NotFoundObjectResult));
    }

    [TestMethod]
    public void GetLnurlPayInfo_ValidConfig_ReturnsOkWithLnurlData()
    {
        var contentMock = new Mock<IPublishedContent>();
        contentMock.Setup(c => c.Name).Returns("Test Content");
        contentMock.Setup(c => c.HasValue("breezPaywall")).Returns(true);
        var paywallJson = JsonSerializer.Serialize(new PaywallConfig { Enabled = true, Fee = 1 });
        contentMock.Setup(c => c.Value<string>("breezPaywall")).Returns(paywallJson);

        var umbracoContextMock = new Mock<IUmbracoContext>();
        umbracoContextMock.Setup(c => c.Content.GetById(1)).Returns(contentMock.Object);

        IUmbracoContext ctx = umbracoContextMock.Object;
        _umbracoContextAccessorMock.Setup(a => a.TryGetUmbracoContext(out ctx)).Returns(true);

        var result = LnurlPayHelper.GetLnurlPayInfo(1, _umbracoContextAccessorMock.Object, _loggerMock.Object, _requestMock.Object, "/callback");

        Assert.IsInstanceOfType(result, typeof(OkObjectResult));
        var okResult = (OkObjectResult)result;
        dynamic data = okResult.Value;
        Assert.AreEqual("payRequest", data.tag);
        Assert.AreEqual("https://test.com/callback?contentId=1", data.callback);
        Assert.AreEqual(1000UL, data.minSendable);
        Assert.AreEqual(1000UL, data.maxSendable);
        Assert.AreEqual("[[\"text/plain\",\"Access to Test Content\"]]", data.metadata);
    }
}