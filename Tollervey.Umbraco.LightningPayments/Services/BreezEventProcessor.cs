using System.Diagnostics;
using System.Threading.Channels;
using Breez.Sdk.Liquid;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Tollervey.Umbraco.LightningPayments.UI.Services.Realtime;

namespace Tollervey.Umbraco.LightningPayments.UI.Services
{
    internal class BreezEventProcessor : IBreezEventProcessor, IHostedService, IDisposable
    {
        private static readonly ActivitySource _activity = new("BreezEventProcessor");
        private readonly ILogger<BreezEventProcessor> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly Channel<SdkEvent.PaymentSucceeded> _queue;
        private Task? _consumerTask;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();

        public BreezEventProcessor(ILogger<BreezEventProcessor> logger, IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
            var options = new BoundedChannelOptions(100)
            {
                FullMode = BoundedChannelFullMode.Wait
            };
            _queue = Channel.CreateBounded<SdkEvent.PaymentSucceeded>(options);
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _consumerTask = Task.Run(async () => await ConsumeQueueAsync(_cts.Token), cancellationToken);
            return Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _cts.Cancel();
            if (_consumerTask != null)
            {
                await _consumerTask;
            }
        }

        public async Task EnqueueEvent(SdkEvent.PaymentSucceeded e)
        {
            if (!_queue.Writer.TryWrite(e))
            {
                _logger.LogWarning("BreezSDK: Event queue is full. Dropping event of type {EventType}.", e.GetType().Name);
                // Optional: Implement a durable store for overflow events here.
                await _queue.Writer.WriteAsync(e);
            }
        }

        private async Task ConsumeQueueAsync(CancellationToken ct)
        {
            await foreach (var e in _queue.Reader.ReadAllAsync(ct))
            {
                using var activity = _activity.StartActivity("OnPaymentSucceeded");
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var deduper = scope.ServiceProvider.GetRequiredService<IPaymentEventDeduper>();
                    var paymentService = scope.ServiceProvider.GetRequiredService<IPaymentStateService>();
                    var sseHub = scope.ServiceProvider.GetRequiredService<SseHub>();

                    string? paymentHash = null;
                    try
                    {
                        var detailsProp = e.GetType().GetProperty("details");
                        if (detailsProp != null)
                        {
                            var details = detailsProp.GetValue(e);
                            if (details != null)
                            {
                                var paymentDetailsProp = details.GetType().GetProperty("details");
                                if (paymentDetailsProp != null)
                                {
                                    var paymentDetails = paymentDetailsProp.GetValue(details);
                                    if (paymentDetails != null)
                                    {
                                        var hashProp = paymentDetails.GetType().GetProperty("paymentHash");
                                        if (hashProp != null)
                                        {
                                            paymentHash = hashProp.GetValue(paymentDetails) as string;
                                        }
                                    }
                                }
                            }
                        }
                        activity?.SetTag("paymentHash", paymentHash);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to extract paymentHash using reflection from SDK event.");
                        activity?.SetStatus(ActivityStatusCode.Error, "Failed to extract paymentHash");
                    }

                    if (string.IsNullOrEmpty(paymentHash))
                    {
                        _logger.LogWarning("Unable to extract paymentHash from PaymentSucceeded event.");
                        continue;
                    }

                    if (!deduper.TryBegin(paymentHash))
                    {
                        _logger.LogInformation("Duplicate payment succeeded event for hash: {PaymentHash}", paymentHash);
                        continue;
                    }

                    await paymentService.ConfirmPaymentAsync(paymentHash);
                    _logger.LogInformation("Confirmed payment in real-time for hash: {PaymentHash}", paymentHash);

                    // Lookup the session to notify and broadcast SSE to the correct browser session
                    var state = await paymentService.GetByPaymentHashAsync(paymentHash);
                    if (state != null && !string.IsNullOrWhiteSpace(state.UserSessionId))
                    {
                        sseHub.Broadcast(state.UserSessionId, "payment-succeeded", new { paymentHash = state.PaymentHash, contentId = state.ContentId, kind = state.Kind.ToString(), status = state.Status.ToString(), amountSat = state.AmountSat });
                    }

                    deduper.Complete(paymentHash);
                    activity?.SetStatus(ActivityStatusCode.Ok);
                }
                catch (Exception ex)
                {
                    activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                    _logger.LogError(ex, "Failed to confirm/broadcast payment from SDK event.");
                }
            }
        }

        public void Dispose()
        {
            _cts.Dispose();
        }
    }
}
