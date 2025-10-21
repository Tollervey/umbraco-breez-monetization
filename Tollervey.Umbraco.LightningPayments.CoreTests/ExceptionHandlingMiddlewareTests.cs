using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Tollervey.Umbraco.LightningPayments.UI.Middleware;

namespace Tollervey.Umbraco.LightningPayments.CoreTests;

[TestClass]
public class ExceptionHandlingMiddlewareTests
{
    private Mock<RequestDelegate> _nextMock;
    private Mock<ILogger<ExceptionHandlingMiddleware>> _loggerMock;
    private DefaultHttpContext _httpContext;
    private ExceptionHandlingMiddleware _middleware;

    [TestInitialize]
    public void Setup()
    {
        _nextMock = new Mock<RequestDelegate>();
        _loggerMock = new Mock<ILogger<ExceptionHandlingMiddleware>>();
        _httpContext = new DefaultHttpContext();
        _middleware = new ExceptionHandlingMiddleware(_nextMock.Object, _loggerMock.Object);
    }

    [TestMethod]
    public async Task InvokeAsync_NoException_CallsNext()
    {
        // Act
        await _middleware.InvokeAsync(_httpContext);

        // Assert
        _nextMock.Verify(next => next(_httpContext), Times.Once);
    }

    [TestMethod]
    public async Task InvokeAsync_WithException_HandlesAndLogs()
    {
        // Arrange
        var exception = new Exception("Test exception");
        _nextMock.Setup(next => next(_httpContext)).ThrowsAsync(exception);
        _httpContext.Response.Body = new MemoryStream();

        // Act
        await _middleware.InvokeAsync(_httpContext);

        // Assert
        Assert.AreEqual(StatusCodes.Status500InternalServerError, _httpContext.Response.StatusCode);

        _httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
        var reader = new StreamReader(_httpContext.Response.Body);
        var responseBody = await reader.ReadToEndAsync();
        Assert.AreEqual("An error occurred. Please try again later.", responseBody);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("An unhandled exception occurred.")),
                exception,
                It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
            Times.Once);
    }
}
