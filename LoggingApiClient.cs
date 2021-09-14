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
        private readonly HttpClient _httpClient;
        private readonly Uri _loggingApiUrl;
        private readonly ILogger<LoggingApiClient> _logger;

        public LoggingApiClient(ILogger<LoggingApiClient> logger, HttpClient httpClient)
        {
            _logger = logger;
            _httpClient = httpClient;

            var runtimeApiAddress = Environment.GetEnvironmentVariable(Configs.LAMBDA_RUNTIME_API_ADDRESS_ENV_VAR);
            _loggingApiUrl = new Uri($"http://{runtimeApiAddress}/2020-08-15/logs");
        }

        public async Task Subscribe(string agendId)
        {
            /*
                https://docs.aws.amazon.com/lambda/latest/dg/runtimes-logs-api.html
            */

            var subscriptionPayload = new
            {
                destination = new
                {
                    protocol = "HTTP",
                    URI = $"http://sandbox.localdomain:{Configs.AGENT_LOGSAPI_PORT}/logging" // apontar para o LogginController!
                },
                types = new string[] { "platform", "function" },
                //types = new string[] { "function" },
                buffering = new
                {
                    timeoutMs = 1000,
                    maxBytes = 262144,
                    maxItems = 1000
                }
            };

            var content = new StringContent(JsonSerializer.Serialize(subscriptionPayload));
            content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");
            content.Headers.Add(Configs.LAMBDA_EXTENSION_ID_HEADER, agendId);
            var response = await _httpClient.PutAsync(_loggingApiUrl, content);
            var responseContent = await response.Content.ReadAsStringAsync();

            _logger.LogInformation($"response: {responseContent}");

            response.EnsureSuccessStatusCode();

            await Task.CompletedTask;
        }
    }
}