using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Poc.LambdaExtension.Logging
{
    public class ExtensionBackgroundWorker
        : BackgroundService
    {
        private readonly ILogger<ExtensionBackgroundWorker> _logger;
        private readonly HttpClient _httpClient;
        private readonly ExtensionClient _extensionClient;
        private readonly LoggingApiClient _loggingApiClient;

        public ExtensionBackgroundWorker(
            ILogger<ExtensionBackgroundWorker> logger,
            HttpClient httpClient,
            ExtensionClient extensionClient,
            LoggingApiClient loggingApiClient
            )
        {
            _logger = logger;
            _httpClient = httpClient;
            _extensionClient = extensionClient;
            _loggingApiClient = loggingApiClient;

        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await _extensionClient.ProcessEvents(
                onInit: async agendId =>
                {
                    await _loggingApiClient.Subscribe(agendId);
                    _logger.LogInformation($"logging api subscribed with agendId '{agendId}'");
                },
                onInvoke: async payload =>
                {
                    _logger.LogInformation($"invoked: {payload}");
                    await Task.CompletedTask;
                },
                onShutdown: payload =>
                {
                    _logger.LogInformation($"shutdown: {payload}");
                    return Task.CompletedTask;
                });
        }
    }
}