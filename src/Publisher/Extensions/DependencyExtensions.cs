using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Publisher.Factories;
using Publisher.Interfaces;
using Publisher.Model;
using Publisher.Services;

namespace Publisher.Extensions
{
    public static class DependencyExtensions
    {
        public static IServiceCollection Register(this IServiceCollection services,IConfiguration configuration)
        {
            services.AddOptions();

            services.Configure<MessagingConfiguration>(configuration.GetSection("Services:Rabbitmq"));

            services.AddTransient<IMessagingService<PingPongExample>, MessagingService<PingPongExample>>();

            services.AddSingleton<IMessagingFactory, MessagingFactory>();

            return services.AddHostedService<HostedService>();
        }
    }
}

