using Consumer.Interfaces;
using Consumer.Model;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client.Events;
using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Consumer.Services
{
    public class ConsumerHostedService : BackgroundService
    {
        private CancellationTokenSource _cancellationTokenSource;
        private Task _executingTask;
        private string _tag;
        private readonly IMessagingFactory _messagingFactory;
        private readonly IMessagingService<PingPongExample> _messagingService;

        private readonly ILogger<ConsumerHostedService> _logger;
        private readonly MessagingConfiguration _messagingConfiguration;

        public ConsumerHostedService(IMessagingFactory messagingFactory, IMessagingService<PingPongExample> messagingService,
            ILogger<ConsumerHostedService> logger, IOptions<MessagingConfiguration> messagingConfiguration)
        {
            _messagingFactory = messagingFactory;
            _messagingService = messagingService;
            _logger = logger;
            _messagingConfiguration = messagingConfiguration.Value;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var channel = _messagingFactory.Initialize();
            var consumer = new AsyncEventingBasicConsumer(channel);

            consumer.Received += _messagingService.Dequeue(stoppingToken, async (raw, message) =>
            {
                if (message == null)
                {
                    return;
                }

                using (_logger.BeginScope(Guid.NewGuid().ToString()))
                {
                    try
                    {

                        var content = JsonSerializer.Serialize(new PingPongExample()
                        {
                            Id = message.Id++,
                            IsPing = true,
                            Message = "Ping"
                        });


                        _messagingService.Queue(new Message()
                        {
                            Exchange = _messagingConfiguration.Publishing.Exchange.Name,
                            RoutingKey = _messagingConfiguration.Publishing.Routingkey,
                            Content = content
                        });

                        _logger.LogInformation("Send ping from consumer");

                    }
                    catch (Exception ex)
                    {
                        _logger.LogCritical($"HOST | CRITICAL ERROR: {ex}");

                        throw;
                    }
                }
            });

            _tag = channel.BasicConsume(_messagingConfiguration.Consuming.Queue, false, string.Empty, false, false,
                null, consumer);

            return Task.CompletedTask;
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            _executingTask = ExecuteAsync(_cancellationTokenSource.Token);

            if (_executingTask.IsCompleted)
            {
                return _executingTask;
            }

            return Task.CompletedTask;
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            if (_executingTask == null)
            {
                return;
            }

            _cancellationTokenSource.Cancel();

            await Task.WhenAny(_executingTask, Task.Delay(-1, cancellationToken)).ConfigureAwait(false);

            var channel = _messagingFactory.Initialize();

            channel.BasicCancel(_tag);

            _messagingFactory.Disconnect();
        }
    }
}
