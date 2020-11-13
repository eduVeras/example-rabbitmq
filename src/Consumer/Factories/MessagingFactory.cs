using Consumer.Interfaces;
using Consumer.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;

namespace Consumer.Factories
{
    public class MessagingFactory : IMessagingFactory
    {
        private readonly IConnectionFactory _connectionFactory;
        private readonly MessagingConfiguration _messaging;
        private IModel _channel;
        private IConnection _connection;
        private readonly ILogger<MessagingFactory> _logger;

        public MessagingFactory(IOptions<MessagingConfiguration> messaging, ILogger<MessagingFactory> logger)
        {

            _messaging = messaging.Value;
            _logger = logger;

            _connectionFactory = new ConnectionFactory
            {
                Uri = new Uri(_messaging.ConnectionString),
                AutomaticRecoveryEnabled = true,
                RequestedHeartbeat = 200,
                DispatchConsumersAsync = true
            };
        }


        private void InitializeHandlers()
        {
            _connection.ConnectionShutdown += OnConnectionShutdown;
            _connection.CallbackException += OnCallbackException;
            _connection.ConnectionBlocked += OnConnectionBlocked;
            _connection.RecoverySucceeded += OnRecoverySucceeded;
            _logger.LogInformation("RabbitMQ Client acquired a persistent connection to '{HostName}' and is subscribed to failure events", _connection.Endpoint.HostName);
        }

        private void OnRecoverySucceeded(object? sender, EventArgs e)
        {
            _logger.LogInformation(
                "[{Classname}].[{MethodName}] - Connection on RabbitMq was RecoverySucceeded. {Date}",
                nameof(MessagingFactory), nameof(OnRecoverySucceeded), DateTime.Now);
        }

        private void OnConnectionBlocked(object? sender, ConnectionBlockedEventArgs e)
        {
            _logger.LogCritical("[{Classname}].[{MethodName}] - Connection on RabbitMq was Blocked, reason: {reason}",
                nameof(MessagingFactory), nameof(OnConnectionBlocked), e.Reason);

            if (_connection != null && _connection.IsOpen)
            {
                return;
            }
        }

        private void OnCallbackException(object? sender, CallbackExceptionEventArgs e)
        {
            if (_connection != null && _connection.IsOpen)
            {
                return;
            }
            _logger.LogError("[{Classname}].[{MethodName}] - Connection on RabbitMq was Exception, reason: {reason}", nameof(MessagingFactory), nameof(OnCallbackException), e.Exception.Message);
        }

        private void OnConnectionShutdown(object? sender, ShutdownEventArgs e)
        {
            if (_connection != null && _connection.IsOpen)
            {
                return;
            }

            _logger.LogError("[{Classname}].[{MethodName}] - Connection on RabbitMq was Shutdown, reason: {reason}", nameof(MessagingFactory), nameof(OnConnectionShutdown), e.Cause);
        }

        public IModel Initialize()
        {
            if (_channel != null)
            {
                return _channel;
            }

            _logger.LogInformation("RABBITMQ | CREATING CONNECTION");
            _connection = _connectionFactory.CreateConnection(_messaging.Hostname);

            _logger.LogInformation("RABBITMQ | CREATING MODEL");
            _channel = _connection.CreateModel();

            InitializeHandlers();

            InitializeConsuming();

            InitializePublishing();

            return _channel;
        }

        public void Disconnect()
        {
            if (_connection.IsOpen)
            {
                _connection.Close();
            }
        }

        private string ExchangeType(string exchangeType)
        {
            switch (exchangeType.ToLower())
            {
                case "direct":
                    return RabbitMQ.Client.ExchangeType.Direct;
                case "fanout":
                    return RabbitMQ.Client.ExchangeType.Fanout;
                case "headers":
                    return RabbitMQ.Client.ExchangeType.Headers;
                case "topic":
                    return RabbitMQ.Client.ExchangeType.Topic;
                default:
                    throw new NotImplementedException("Exchange type not implemented");
            }
        }

        private void InitializeConsuming()
        {
            _logger.LogInformation("RABBITMQ | CREATING DEADLETTER EXCHANGE: {_messagingConsumingDeadletterExchange}",
               _messaging.Consuming.Deadletter.Exchange.Name);

            _channel.ExchangeDeclare(_messaging.Consuming.Deadletter.Exchange.Name, ExchangeType(_messaging.Consuming.Exchange.Type), true);

            _logger.LogInformation("RABBITMQ | CREATING DEADLETTER QUEUE: {_messagingConsumingDeadletterQueue}",
                _messaging.Consuming.Deadletter.Queue);

            _channel.QueueDeclare(_messaging.Consuming.Deadletter.Queue, true, false, false, new Dictionary<string, object>()
            {
                { "x-dead-letter-exchange", _messaging.Consuming.Exchange.Name },
                { "x-dead-letter-routing-key", _messaging.Consuming.Bindingkey },
                { "x-message-ttl", _messaging.Ttl }
            });

            _logger.LogInformation("RABBITMQ | BINDING DEADLETTER EXCHANGE AND QUEUE");

            _channel.QueueBind(_messaging.Consuming.Deadletter.Queue, _messaging.Consuming.Deadletter.Exchange.Name, _messaging.Consuming.Deadletter.Routingkey);

            // The queue this service will watch for new messages
            _logger.LogInformation("RABBITMQ | CREATING CONSUMING EXCHANGE: {_messagingConsumingExchange}",
                _messaging.Consuming.Exchange.Name);

            _channel.ExchangeDeclare(_messaging.Consuming.Exchange.Name, ExchangeType(_messaging.Consuming.Exchange.Type), true);

            _logger.LogInformation("RABBITMQ | CREATING CONSUMING QUEUE: {_messagingConsumingQueue}",
                _messaging.Consuming.Queue);

            _channel.QueueDeclare(_messaging.Consuming.Queue, true, false, false, new Dictionary<string, object>()
            {
                { "x-dead-letter-exchange", _messaging.Consuming.Deadletter.Exchange.Name },
                { "x-dead-letter-routing-key", _messaging.Consuming.Deadletter.Routingkey }
            });

            _logger.LogInformation("RABBITMQ | BINDING CONSUMING EXCHANGE AND QUEUE");
            _channel.QueueBind(_messaging.Consuming.Queue, _messaging.Consuming.Exchange.Name, _messaging.Consuming.Bindingkey);
        }

        private void InitializePublishing()
        {
            _logger.LogInformation("RABBITMQ | CREATING POSTING EXCHANGE: {_messagingPublishingExchange}", _messaging.Publishing.Exchange.Name);

            _channel.ExchangeDeclare(_messaging.Publishing.Exchange.Name, ExchangeType(_messaging.Publishing.Exchange.Type), true);

            _logger.LogInformation("RABBITMQ | CREATING POSTING QUEUE: {_messagingPublishingQueue}", _messaging.Publishing.Queue);

            _channel.QueueDeclare(_messaging.Publishing.Queue, true, false, false, new Dictionary<string, object>()
            {
                {"x-dead-letter-exchange", _messaging.Publishing.Exchange.Name},
                {"x-dead-letter-routing-key", _messaging.Publishing.Routingkey}
            });

            _logger.LogInformation("RABBITMQ | BINDING POSTING EXCHANGE AND QUEUE");
            _channel.QueueBind(_messaging.Publishing.Queue, _messaging.Publishing.Exchange.Name, _messaging.Publishing.Routingkey);
        }
    }
}
