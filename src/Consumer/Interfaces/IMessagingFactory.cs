using RabbitMQ.Client;

namespace Consumer.Interfaces
{
    public interface IMessagingFactory
    {
        IModel Initialize();
        void Disconnect();
    }
}
