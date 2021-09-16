using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Poc.LambdaExtension.Logging
{
    public class Program
    {
        static async Task Main(string[] args)
        {
            await CreateHostBuilder(args).Build().RunAsync();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureLogging(logging =>
                    logging.AddFilter((provider, category, logLevel) =>
                    {
                        return category.Contains("Poc.LambdaExtension.Logging") && logLevel >= LogLevel.Information;
                    }))
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                    webBuilder.UseUrls($"http://+:{Configs.AGENT_LOGSAPI_PORT}");
                })
                .ConfigureServices(services => 
                {
                    services.AddHttpClient<LogsEventsProcessingWorker>();
                    services.AddHostedService<LogsEventsProcessingWorker>();
                });
    }
}
