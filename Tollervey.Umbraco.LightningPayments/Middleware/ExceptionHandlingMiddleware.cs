using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Tollervey.Umbraco.LightningPayments.UI.Middleware
{
    /// <summary>
    /// Middleware that returns a minimal, user-friendly error response for unhandled exceptions
    /// on the public website pipeline, without affecting Umbraco backoffice or API routes.
    /// </summary>
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        private static readonly PathString BackofficePrefix = new("/umbraco");
        private static readonly PathString PaywallSurfacePrefix = new("/umbraco/surface/paywallsurface");
        private static readonly PathString ApiPrefix = new("/api");

        /// <summary>
        /// Initializes a new instance of the <see cref="ExceptionHandlingMiddleware"/> class.
        /// </summary>
        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        /// <summary>
        /// Processes the HTTP request and converts unhandled exceptions into a simple JSON or text response
        /// depending on the request's Accept header.
        /// </summary>
        public async Task InvokeAsync(HttpContext context)
        {
            // Only handle public website requests; let backoffice/management and APIs handle themselves
            if (ShouldBypass(context))
            {
                await _next(context);
                return;
            }

            try
            {
                await _next(context);
            }
            catch (OperationCanceledException)
            {
                // Common during client disconnects; don't spam error logs
                _logger.LogInformation("Request was canceled by the client.");
                throw;
            }
            catch (Exception ex)
            {
                // If the response has already started we can't change it; just log and rethrow
                if (context.Response.HasStarted)
                {
                    _logger.LogError(ex, "Unhandled exception after response started.");
                    throw;
                }

                _logger.LogError(ex, "An unhandled exception occurred while processing the request.");

                // Return a generic error tailored to Accept header
                var traceId = context.TraceIdentifier;
                var genericMessage = "An error occurred. Please try again later.";

                if (WantsJson(context))
                {
                    context.Response.Clear();
                    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                    context.Response.ContentType = "application/json; charset=utf-8";

                    var payload = new
                    {
                        error = genericMessage,
                        traceId
                    };

                    await context.Response.WriteAsync(JsonSerializer.Serialize(payload));
                }
                else
                {
                    context.Response.Clear();
                    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                    context.Response.ContentType = "text/plain; charset=utf-8";
                    await context.Response.WriteAsync($"{genericMessage} (TraceId: {traceId})");
                }
            }
        }

        private static bool ShouldBypass(HttpContext context)
        {
            var path = context.Request.Path;

            // Skip for backoffice/management/API and our own surface endpoint
            if (path.StartsWithSegments(PaywallSurfacePrefix, StringComparison.OrdinalIgnoreCase))
                return true;

            if (path.StartsWithSegments(BackofficePrefix, StringComparison.OrdinalIgnoreCase))
                return true;

            // Bypass all /api/* routes (public and management controllers handle JSON envelopes themselves)
            if (path.StartsWithSegments(ApiPrefix, StringComparison.OrdinalIgnoreCase))
                return true;

            return false;
        }

        private static bool WantsJson(HttpContext context)
        {
            var accept = context.Request.Headers["Accept"].ToString();
            if (string.IsNullOrWhiteSpace(accept)) return false;

            // Prefer JSON when client explicitly asks for it and not text/html
            return accept.Contains("application/json", StringComparison.OrdinalIgnoreCase)
                   && !accept.Contains("text/html", StringComparison.OrdinalIgnoreCase);
        }
    }
}