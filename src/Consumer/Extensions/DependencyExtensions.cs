using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace Consumer.Extensions
{
    public static class DependencyExtensions
    {
        public static IServiceCollection Register(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddOptions();

            //services.Configure<MessagingConfiguration>(configuration.GetSection("Services:Rabbitmq"));

            //services.AddTransient<IMessagingService<PingPongExample>, MessagingService<PingPongExample>>();

            //services.AddSingleton<IMessagingFactory, MessagingFactory>();

            //services.AddHostedService<HostedService>();

            //return services.AddHostedService<HostedService>();

            return services;
        }
    }
}
