using System.Diagnostics;
using System.Text.Json;
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
        private readonly Channel<SdkEvent> _eventQueue = Channel.CreateUnbounded<SdkEvent>();
        private Task? _consumerTask;
        private Task? _eventConsumerTask;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();

        public BreezEventProcessor(ILogger<BreezEventProcessor> logger, IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
            var options = new BoundedChannelOptions(100) { FullMode = BoundedChannelFullMode.Wait };
            _queue = Channel.CreateBounded<SdkEvent.PaymentSucceeded>(options);
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _consumerTask = Task.Run(async () => await ConsumeQueueAsync(_cts.Token), cancellationToken);
            _eventConsumerTask = Task.Run(async () => await ConsumeGeneralEventsAsync(_cts.Token), cancellationToken);
            return Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _cts.Cancel();
            if (_consumerTask != null) await _consumerTask;
            if (_eventConsumerTask != null) await _eventConsumerTask;
        }

        public async Task EnqueueEvent(SdkEvent.PaymentSucceeded e)
        {
            if (!_queue.Writer.TryWrite(e))
            {
                _logger.LogWarning("BreezSDK: Event queue is full. Dropping PaymentSucceeded event.");
                await _queue.Writer.WriteAsync(e);
            }
        }

        public async Task Enqueue(SdkEvent e)
        {
            await _eventQueue.Writer.WriteAsync(e);
        }

        private async Task ConsumeGeneralEventsAsync(CancellationToken ct)
        {
            await foreach (var e in _eventQueue.Reader.ReadAllAsync(ct))
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var sseHub = scope.ServiceProvider.GetRequiredService<SseHub>();
                    var payload = new { type = e.GetType().Name, details = e.ToString() };
                    // Broadcast to all connected sessions (use a special key "*" to mean broadcast-all)
                    sseHub.Broadcast("*", "breez-event", payload);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to broadcast Breez SDK event.");
                }
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

                    // Confirm and broadcast. If duplicate events arrive, calling ConfirmPaymentAsync for an already-paid
                    // hash is idempotent and returns AlreadyConfirmed, so a separate deduper is unnecessary.
                    var result = await paymentService.ConfirmPaymentAsync(paymentHash);
                    _logger.LogInformation("PaymentSucceeded processed for hash: {PaymentHash} => {Result}", paymentHash, result);

                    var state = await paymentService.GetByPaymentHashAsync(paymentHash);
                    if (state != null && !string.IsNullOrWhiteSpace(state.UserSessionId))
                    {
                        sseHub.Broadcast(state.UserSessionId, "payment-succeeded", new { paymentHash = state.PaymentHash, contentId = state.ContentId, kind = state.Kind.ToString(), status = state.Status.ToString(), amountSat = state.AmountSat });
                    }

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
