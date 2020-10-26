using RabbitMQ.Client;

namespace Publisher.Interfaces
{
    public interface IMessagingFactory
    {
        IModel Initialize();
        void Disconnect();
    }
}
