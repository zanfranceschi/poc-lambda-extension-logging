using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Poc.LambdaExtension.Logging
{
    public class LoggingApiClient
    {
        private const string LAMBDA_EXTENSION_ID_HEADER = "Lambda-Extension-Identifier";
        private const string LAMBDA_EXTENSION_NAME_HEADER = "Lambda-Extension-Name";
        private const string LAMBDA_RUNTIME_API_ADDRESS = "AWS_LAMBDA_RUNTIME_API";
        private readonly HttpClient _httpClient;
        private readonly Uri _loggingApiUrl;
        private readonly ILogger<LoggingApiClient> _logger;

        public LoggingApiClient(ILogger<LoggingApiClient> logger, HttpClient httpClient)
        {
            _logger = logger;
            _httpClient = httpClient;

            var runtimeApiAddress = Environment.GetEnvironmentVariable(LAMBDA_RUNTIME_API_ADDRESS);
            _loggingApiUrl = new Uri($"http://{runtimeApiAddress}/2020-08-15/logs");
        }

        public async Task Subscribe(string agendId)
        {
            /*
                https://docs.aws.amazon.com/lambda/latest/dg/runtimes-logs-api.html
            */

            _logger.LogInformation($"subscribing to logs API with agentId {agendId}...");

            var subscriptionPayload = new
            {
                destination = new
                {
                    protocol = "HTTP",
                    URI = $"http://sandbox.localdomain:{Configs.AGENT_LOGSAPI_PORT}/logging" // apontar para o LogginController!
                },
                types = new string[] { "platform", "function" },
                buffering = new
                {
                    /*
                        timeoutMs – The maximum time (in milliseconds) to buffer a batch. Default: 1,000. Minimum: 25. Maximum: 30,000.
                        maxBytes – The maximum size (in bytes) of the logs to buffer in memory. Default: 262,144. Minimum: 262,144. Maximum: 1,048,576.
                        maxItems – The maximum number of events to buffer in memory. Default: 10,000. Minimum: 1,000. Maximum: 10,000.
                    */

                    timeoutMs = 1000,
                    maxBytes = 262144,
                    maxItems = 1000
                }
            };

            var content = new StringContent(JsonSerializer.Serialize(subscriptionPayload));
            content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");
            content.Headers.Add(LAMBDA_EXTENSION_ID_HEADER, agendId);
            var response = await _httpClient.PutAsync(_loggingApiUrl, content);
            var responseContent = await response.Content.ReadAsStringAsync();

            _logger.LogInformation($"response: {responseContent}");

            response.EnsureSuccessStatusCode();

            _logger.LogInformation($"subscribed to logs API.");

            await Task.CompletedTask;
        }
    }
}