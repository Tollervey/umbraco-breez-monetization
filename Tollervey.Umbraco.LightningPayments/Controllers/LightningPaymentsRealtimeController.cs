using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Tollervey.Umbraco.LightningPayments.UI.Middleware;
using Tollervey.Umbraco.LightningPayments.UI.Services.Realtime;

namespace Tollervey.Umbraco.LightningPayments.UI.Controllers
{
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

 Response.Headers.Add("Content-Type", "text/event-stream");
 Response.Headers.Add("Cache-Control", "no-cache");
 Response.Headers.Add("X-Accel-Buffering", "no");
 Response.StatusCode = StatusCodes.Status200OK;

 var client = _hub.AddClient(sessionId);
 _logger.LogInformation("SSE connected: {ClientId} for session {SessionId}", client.Id, sessionId);
 try
 {
 await SseHub.WriteStreamAsync(Response, client.Outbound.Reader, HttpContext.RequestAborted);
 }
 finally
 {
 _hub.RemoveClient(sessionId, client.Id);
 _logger.LogInformation("SSE disconnected: {ClientId} for session {SessionId}", client.Id, sessionId);
 }
 }
 }
}
