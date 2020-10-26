using System;
using System.Threading;
using System.Threading.Tasks;
using Publisher.Model;
using RabbitMQ.Client.Events;

namespace Publisher.Interfaces
{
    public interface IMessagingService<out T>
    {
        AsyncEventHandler<BasicDeliverEventArgs> Dequeue(CancellationToken cancellationToken, Func<string, T, Task> callback);
        void Queue(Message message);
    }
}