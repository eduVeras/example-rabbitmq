using Consumer.Model;
using RabbitMQ.Client.Events;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Consumer.Interfaces
{
    public interface IMessagingService<T>
    {
        AsyncEventHandler<BasicDeliverEventArgs> Dequeue(CancellationToken cancellationToken, Func<string, T, Task> callback);
        void Queue(Message message);
    }
}
