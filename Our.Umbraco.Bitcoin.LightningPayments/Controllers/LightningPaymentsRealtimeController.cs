using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Our.Umbraco.Bitcoin.LightningPayments.Middleware;
using Our.Umbraco.Bitcoin.LightningPayments.Services.Realtime;

namespace Our.Umbraco.Bitcoin.LightningPayments.Controllers
{
 /// <summary>
 /// Server-Sent Events (SSE) endpoint for real-time payment updates to the front end.
 /// </summary>
 [ApiController]
 [Route("api/public/lightning/realtime")]
 [RequireHttps]
 public class LightningPaymentsRealtimeController : ControllerBase
 {
 private readonly ILogger<LightningPaymentsRealtimeController> _logger;
 private readonly SseHub _hub;

 public LightningPaymentsRealtimeController(ILogger<LightningPaymentsRealtimeController> logger, SseHub hub)
 {
 _logger = logger;
 _hub = hub;
 }

 /// <summary>
 /// Subscribes the current session to receive real-time events via SSE.
 /// </summary>
 [HttpGet("subscribe")] // GET /api/public/lightning/realtime/subscribe
 public async Task Subscribe()
 {
 var sessionId = Request.Cookies[PaywallMiddleware.PaywallCookieName];
 if (string.IsNullOrEmpty(sessionId))
 {
 Response.StatusCode = StatusCodes.Status401Unauthorized;
 await Response.WriteAsync("No session.");
 return;
 }

 // Proper SSE headers
 Response.ContentType = "text/event-stream";
 Response.Headers["Cache-Control"] = "no-cache";
 Response.Headers["Connection"] = "keep-alive";
 Response.Headers["X-Accel-Buffering"] = "no"; // disable nginx buffering
 Response.StatusCode = StatusCodes.Status200OK;

 // Disable server buffering if available to ensure low-latency writes
 HttpContext.Features.Get<IHttpResponseBodyFeature>()?.DisableBuffering();

 var client = _hub.AddClient(sessionId);
 _logger.LogInformation("SSE connected: {ClientId} for session {SessionId}", client.Id, sessionId);

 // Send an initial heartbeat to establish the stream
 try
 {
 await Response.WriteAsync(":\n\n");
 await Response.Body.FlushAsync(HttpContext.RequestAborted);
 }
 catch (OperationCanceledException)
 {
 // client disconnected before stream established
 }
 catch (Exception ex)
 {
 _logger.LogError(ex, "Failed sending initial SSE heartbeat for client {ClientId}", client.Id);
 }

 try
 {
 await SseHub.WriteStreamAsync(Response, client.Outbound.Reader, HttpContext.RequestAborted);
 }
 catch (OperationCanceledException)
 {
 _logger.LogInformation("SSE client disconnected (canceled): {ClientId} for session {SessionId}", client.Id, sessionId);
 }
 catch (Exception ex)
 {
 _logger.LogError(ex, "SSE stream error for client {ClientId} in session {SessionId}", client.Id, sessionId);
 }
 finally
 {
 _hub.RemoveClient(sessionId, client.Id);
 _logger.LogInformation("SSE disconnected: {ClientId} for session {SessionId}", client.Id, sessionId);
 }
 }
 }
}

