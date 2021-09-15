using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;

namespace Poc.LambdaExtension.Logging
{
    public enum ExtensionEvent
    {
        INVOKE,
        SHUTDOWN,
    }

    /// <summary>
    /// Lambda Extension API client
    /// </summary>
    public class ExtensionClient
    {
        private const string LAMBDA_EXTENSION_NAME_HEADER = "Lambda-Extension-Name";
        private const string LAMBDA_EXTENSION_FUNCTION_ERROR_TYPE_HEADER = "Lambda-Extension-Function-Error-Type";
        private const string LAMBDA_EXTENSION_ID_HEADER = "Lambda-Extension-Identifier";
        private const string LAMBDA_RUNTIME_API_ADDRESS = "AWS_LAMBDA_RUNTIME_API";
        
        public string Id { get; private set; }
        private readonly HttpClient _httpClient;
        private readonly string _extensionName;
        private readonly Uri _registerUrl;
        private readonly Uri _nextUrl;
        private readonly Uri _initErrorUrl;
        private readonly Uri _shutdownErrorUrl;

        public ExtensionClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _httpClient.Timeout = Timeout.InfiniteTimeSpan;

            _extensionName = "poc-lambda-extension-logging";

            // Set infinite timeout so that underlying connection is kept alive
            _httpClient.Timeout = Timeout.InfiniteTimeSpan;
            // Get Extension API service base URL from the environment variable
            var apiUri = new UriBuilder(Environment.GetEnvironmentVariable(LAMBDA_RUNTIME_API_ADDRESS)).Uri;
            // Common path for all Extension API URLs
            var basePath = "2020-01-01/extension";

            // Calculate all Extension API endpoints' URLs
            _registerUrl = new Uri(apiUri, $"{basePath}/register");
            _nextUrl = new Uri(apiUri, $"{basePath}/event/next");
            _initErrorUrl = new Uri(apiUri, $"{basePath}/init/error");
            _shutdownErrorUrl = new Uri(apiUri, $"{basePath}/exit/error");
        }

        public async Task ProcessEvents(
            Func<string, Task> onInit = null,
            Func<string, Task> onInvoke = null,
            Func<string, Task> onShutdown = null)
        {
            await RegisterExtensionAsync(ExtensionEvent.INVOKE, ExtensionEvent.SHUTDOWN);
            
            // If onInit function is defined, invoke it and report any unhandled exceptions
            if (!await SafeInvoke(onInit, Id, ex => ReportErrorAsync(_initErrorUrl, "Fatal.Unhandled", ex))) return;

            // loop till SHUTDOWN event is received
            var hasNext = true;
            while (hasNext)
            {
                // get the next event type and details
                var (type, payload) = await GetNextAsync();

                switch (type)
                {
                    case ExtensionEvent.INVOKE:
                        // invoke onInit function if one is defined and log unhandled exceptions
                        // event loop will continue even if there was an exception
                        await SafeInvoke(onInvoke, payload, onException: ex =>
                        {
                            Console.WriteLine($"[{_extensionName}] Invoke handler threw an exception: {ex}");
                            return Task.CompletedTask;
                        });
                        break;
                    case ExtensionEvent.SHUTDOWN:
                        // terminate the loop, invoke onShutdown function if there is any and report any unhandled exceptions to AWS Extension API
                        hasNext = false;
                        await SafeInvoke(onShutdown, Id, ex => ReportErrorAsync(_shutdownErrorUrl, "Fatal.Unhandled", ex));
                        break;
                    default:
                        throw new ApplicationException($"Unexpected event type: {type}");
                }
            }
        }

        private async Task<string> RegisterExtensionAsync(params ExtensionEvent[] events)
        {
            // custom options for JsonSerializer to serialize ExtensionEvent enum values as strings, rather than integers
            // thus we produce strongly typed code, which doesn't rely on strings
            var options = new JsonSerializerOptions();
            options.Converters.Add(new JsonStringEnumConverter());

            // create Json content for this extension registration
            //ExtensionEvent[] events = new ExtensionEvent[] { ExtensionEvent.INVOKE, ExtensionEvent.SHUTDOWN };
            using var content = new StringContent(JsonSerializer.Serialize(new
            {
                events
            }, options), Encoding.UTF8, "application/json");

            // add extension name header value
            content.Headers.Add(LAMBDA_EXTENSION_NAME_HEADER, _extensionName);

            // POST call to Extension API
            using var response = await _httpClient.PostAsync(_registerUrl, content);

            // if POST call didn't succeed
            if (!response.IsSuccessStatusCode)
            {
                // log details
                Console.WriteLine($"[{_extensionName}] Error response received for registration request: {await response.Content.ReadAsStringAsync()}");
                // throw an unhandled exception, so that extension is terminated by Lambda runtime
                response.EnsureSuccessStatusCode();
            }

            // get registration id from the response header
            Id = response.Headers.GetValues(LAMBDA_EXTENSION_ID_HEADER).FirstOrDefault();
            // if registration id is empty
            if (string.IsNullOrEmpty(Id))
            {
                // throw an exception
                throw new ApplicationException("Extension API register call didn't return a valid identifier.");
            }
            // configure all HttpClient to send registration id header along with all subsequent requests
            _httpClient.DefaultRequestHeaders.Add(LAMBDA_EXTENSION_ID_HEADER, Id);

            return Id;
        }

        /// <summary>
        /// Long poll for the next event from Extension API
        /// </summary>
        /// <returns>Awaitable tuple having event type and event details fields</returns>
        /// <remarks>It is important to have httpClient.Timeout set to some value, that is longer than any expected wait time,
        /// otherwise HttpClient will throw an exception when getting the next event details from the server.</remarks>
        private async Task<(ExtensionEvent type, string payload)> GetNextAsync()
        {
            // use GET request to long poll for the next event
            var contentBody = await _httpClient.GetStringAsync(_nextUrl);

            // use JsonDocument instead of JsonSerializer, since there is no need to construct the entire object
            using var doc = JsonDocument.Parse(contentBody);

            // extract eventType from the reply, convert it to ExtensionEvent enum and reply with the typed event type and event content details.
            return new(Enum.Parse<ExtensionEvent>(doc.RootElement.GetProperty("eventType").GetString()), contentBody);
        }

        private async Task ReportErrorAsync(Uri url, string errorType, Exception exception)
        {
            using var content = new StringContent(string.Empty);
            content.Headers.Add(LAMBDA_EXTENSION_ID_HEADER, Id);
            content.Headers.Add(LAMBDA_EXTENSION_FUNCTION_ERROR_TYPE_HEADER, $"{errorType}.{exception.GetType().Name}");

            using var response = await _httpClient.PostAsync(url, content);
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"[{_extensionName}] Error response received for {url.PathAndQuery}: {await response.Content.ReadAsStringAsync()}");
                response.EnsureSuccessStatusCode();
            }
        }

        private async Task<bool> SafeInvoke(Func<string, Task> func, string param, Func<Exception, Task> onException)
        {
            try
            {
                await func?.Invoke(param);
                return true;
            }
            catch (Exception ex)
            {
                await onException?.Invoke(ex);
                return false;
            }
        }
    }
}