using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Polly;
using Consumer.Interfaces;
using Consumer.Model;
using RabbitMQ.Client.Events;

namespace Consumer.Services
{
    public class MessagingService<T> : IMessagingService<T>
    {
        private readonly IMessagingFactory _messagingFactory;
        private readonly ILogger<MessagingService<T>> _logger;
        private readonly MessagingConfiguration _messagingConfiguration;

        public MessagingService(IOptions<MessagingConfiguration> messagingConfiguration, ILogger<MessagingService<T>> logger, IMessagingFactory messagingFactory)
        {
            _messagingConfiguration = messagingConfiguration.Value;
            _logger = logger;
            _messagingFactory = messagingFactory;
        }

        public AsyncEventHandler<BasicDeliverEventArgs> Dequeue(CancellationToken cancellationToken,
            Func<string, T, Task> callback)
        {

            cancellationToken.ThrowIfCancellationRequested();

            if (callback == null)
            {
                throw new ArgumentNullException(nameof(callback));
            }

            var channel = _messagingFactory.Initialize();

            return async (model, ea) =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                _logger.LogDebug("MESSAGING | RECEIVING NEW MESSAGE");

                var body = ea.Body;
                var raw = Encoding.UTF8.GetString(body);
                var retries = 0;

                _logger.LogDebug("MESSAGING | RAW MESSAGE: {raw}");

                await Policy
                    .Handle<Exception>()
                    .Fallback((app) => { },
                        onFallback: (context, expcetion) =>
                        {
                            _logger.LogDebug("MESSAGING | SOMETHING HAPPENED WHEN PROCESSING THE MESSAGE: {ex}",
                                context.Message);

                            if (_messagingConfiguration.Retries <= retries)
                            {
                                return;
                            }

                            channel.BasicNack(ea.DeliveryTag, false, false);

                            _logger.LogDebug("MESSAGING | MESSAGE HAS BEEN NACKED");

                        }).Execute(async () =>
                        {
                            retries = Retries(ea);

                            _logger.LogDebug("MESSAGING | THIS MESSAGE HAS BEEN PROCESSED { retries } TIMES");

                            var message = JsonConvert.DeserializeObject<T>(raw);

                            await callback.Invoke(raw, message).ConfigureAwait(false);

                            _logger.LogDebug("MESSAGING | MESSAGE WILL BE ACKED");

                            channel.BasicAck(ea.DeliveryTag, false);

                            _logger.LogDebug("MESSAGING | MESSAGE HAS BEEN ACKED");
                        });

            };
        }

        public void Queue(Message message)
        {
            var channel = _messagingFactory.Initialize();

            var properties = channel.CreateBasicProperties();
            properties.Headers = message.Header;
            properties.Persistent = true;

            _logger.LogDebug("MESSAGING | PUSHING '{message}' TO ROUTING KEY '{routingKey}' ON '{exchange}' EXCHANGE",
                message.Content, message.RoutingKey, message.Exchange);

            channel.BasicPublish(message.Exchange, message.RoutingKey, false, properties, message.GetContentBeforeSend());

            _logger.LogDebug("MESSAGING | MESSAGE WAS SUCCESSFULLY PUSHED");
        }

        private int Retries(BasicDeliverEventArgs ea)
        {
            int count = 0;

            try
            {
                if (ea.BasicProperties.Headers is Dictionary<string, object> dic && dic.ContainsKey("x-death"))
                {
                    if (ea.BasicProperties.Headers["x-death"] is List<object> xdeath)
                    {
                        if (xdeath.FirstOrDefault() is Dictionary<string, object> headers)
                        {
                            count = Convert.ToInt32(headers["count"]);
                        }
                    }
                }
            }
            catch
            {
                count = 1;
            }

            return ++count;
        }
    }
}
