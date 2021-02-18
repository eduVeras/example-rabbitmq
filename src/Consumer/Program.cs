using System;
using System.Threading.Tasks;
using Consumer.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace Consumer
{
    class Program
    {
        public static IHost BuildHost(string[] args) => new HostBuilder()
            .ConfigureAppConfiguration((hostContext, configuration) =>
            {
                configuration
                    .AddEnvironmentVariables()
                    .AddJsonFile("appsettings.json", false, true);

                var config = configuration.Build();
                hostContext.HostingEnvironment.EnvironmentName = config.GetValue<string>("Serilog:Properties:Environment");
                hostContext.HostingEnvironment.ApplicationName = config.GetValue<string>("Serilog:Properties:Application");

                Log.Logger = new LoggerConfiguration()
                    .Enrich.FromLogContext()
                    .ReadFrom.Configuration(config)
                    .CreateLogger();
            })
            .ConfigureServices((hostContext, services) => {
                services.Register(hostContext.Configuration);
            })
            .UseSerilog()
            .Build();

        public static async Task Main(string[] args)
        {
            try
            {
                var host = BuildHost(args);

                using (host)
                {
                    await host.StartAsync().ConfigureAwait(false);

                    await host.WaitForShutdownAsync().ConfigureAwait(false);
                }
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }
    }
}
