using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Publisher.Interfaces;
using Publisher.Model;
using RabbitMQ.Client.Events;
using System.Text.Json;

namespace Publisher.Services
{
    public class HostedService : BackgroundService
    {
        private CancellationTokenSource _cancellationTokenSource;
        private Task _executingTask;
        private string _tag;
        private readonly IMessagingFactory _messagingFactory;
        private readonly IMessagingService<PingPongExample> _messagingService;

        private readonly ILogger<HostedService> _logger;
        private readonly MessagingConfiguration _messagingConfiguration;

        public HostedService(IMessagingFactory messagingFactory, IMessagingService<PingPongExample> messagingService,
            ILogger<HostedService> logger, IOptions<MessagingConfiguration> messagingConfiguration)
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
                            Message = $"Pong {message.Id}"
                        });


                        _messagingService.Queue(new Message()
                        {
                            Exchange = _messagingConfiguration.Publishing.Exchange.Name,
                            RoutingKey = _messagingConfiguration.Publishing.Routingkey,
                            Content = content
                        });

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
            _logger.LogInformation("Inicilizando a aplicação Publisher");

            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            _executingTask = ExecuteAsync(_cancellationTokenSource.Token);

            FirstPong();

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

        public void FirstPong()
        {
            var startPong = new PingPongExample()
            {
                Id = 1,
                IsPing = true,
                Message = "Pong"
            };

            _messagingService.Queue(new Message()
            {
                Exchange = _messagingConfiguration.Publishing.Exchange.Name,
                RoutingKey = _messagingConfiguration.Publishing.Routingkey,
                Content = JsonSerializer.Serialize(startPong)
            });
        }
    }
}
