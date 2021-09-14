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
        private const string LAMBDA_RUNTIME_API_ADDRESS = "AWS_LAMBDA_RUNTIME_API";
        private readonly HttpClient _httpClient;
        private readonly Uri _loggingApiUrl;
        private readonly ILogger<LoggingApiClient> _logger;

        public LoggingApiClient(ILogger<LoggingApiClient> logger, HttpClient httpClient)
        {
            _logger = logger;
            _httpClient = httpClient;

            var runtimeApiAddress  = Environment.GetEnvironmentVariable(LAMBDA_RUNTIME_API_ADDRESS);
            _loggingApiUrl = new Uri($"http://{runtimeApiAddress}/2020-08-15/logs");
        }

        public async Task Subscribe(string agendId) 
        {
            var content = new StringContent(@"
                {
                    ""destination"":{
                        ""protocol"": ""HTTP"",
                        ""URI"": ""http://sandbox:9999"",
                    },
                    ""types"": [""platform"", ""function""],
                    ""buffering"": {
                        ""timeoutMs"": 1000,
                        ""maxBytes"": 262144,
                        ""maxItems"": 10000
                    }");
            content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");
            content.Headers.Add(LAMBDA_EXTENSION_ID_HEADER, agendId);
            var response = await _httpClient.PutAsync(_loggingApiUrl, content);

            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();

            _logger.LogInformation($"response: {responseContent}");

            await Task.CompletedTask;
        }
    }
}