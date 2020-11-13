using Consumer.Factories;
using Consumer.Interfaces;
using Consumer.Model;
using Consumer.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Consumer.Extensions
{
    public static class DependencyExtensions
    {
        public static IServiceCollection Register(this IServiceCollection services, IConfiguration configuration) => 
            services.AddOptions()
                .Configure<MessagingConfiguration>(configuration.GetSection("Services:Rabbitmq"))
                .AddTransient<IMessagingService<PingPongExample>, MessagingService<PingPongExample>>()
                .AddSingleton<IMessagingFactory, MessagingFactory>()
                .AddHostedService<ConsumerHostedService>();
    }
}
