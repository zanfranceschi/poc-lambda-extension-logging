using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Unicode;
using System.Threading;
using System.Threading.Tasks;
using Amazon.Kinesis;
using Amazon.KinesisFirehose;
using Amazon.KinesisFirehose.Model;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Poc.LambdaExtension.Logging
{
    public class LogsEventsProcessingWorker
        : BackgroundService
    {
        private readonly ILogger<LogsEventsProcessingWorker> _logger;
        private readonly HttpClient _httpClient;
        private readonly ExtensionClient _extensionClient;
        private readonly LoggingApiClient _loggingApiClient;
        private readonly ConcurrentQueue<string> _logsQueue;
        private readonly IAmazonS3 _s3Client;
        private readonly IAmazonKinesisFirehose _kinesisFirehoseClient;
        private readonly IAmazonKinesis _kinesisClient;
        private readonly string _bucketName;
        private readonly string _bucketKeyPrefeix;
        private readonly string _functionName;

        public LogsEventsProcessingWorker(
            ILogger<LogsEventsProcessingWorker> logger,
            HttpClient httpClient,
            ExtensionClient extensionClient,
            LoggingApiClient loggingApiClient,
            IAmazonS3 s3Client,
            IAmazonKinesisFirehose kinesisFirehoseClient,
            IAmazonKinesis kinesisClient,
            ConcurrentQueue<string> logsQueue
            )
        {
            _logger = logger;
            _httpClient = httpClient;
            _extensionClient = extensionClient;
            _loggingApiClient = loggingApiClient;

            _s3Client = s3Client;
            _kinesisFirehoseClient = kinesisFirehoseClient;
            _kinesisClient = kinesisClient;
            _logsQueue = logsQueue;

            _bucketName = "zanfranceschi";
            _bucketKeyPrefeix = "logs";
            _functionName = Environment.GetEnvironmentVariable("AWS_LAMBDA_FUNCTION_NAME") ?? "sem-nome";
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await _extensionClient.ProcessEvents(
                onInit: async agentId =>
                {
                    await _loggingApiClient.Subscribe(agentId);
                    _logger.LogInformation($"logging api subscribed with agentId '{agentId}'");
                },
                onInvoke: async payload =>
                {
                    await ProcessLogsEvents();
                },
                onShutdown: payload =>
                {
                    _logger.LogInformation($"shutdown: {payload}");
                    return ProcessLogsEvents();
                });
        }

        private async Task ProcessLogsEvents()
        {
            string logsEventPayload;
            while (_logsQueue.TryDequeue(out logsEventPayload))
            {
                try
                {
                    // S3
                    _logger.LogInformation($"executing ProcessLogsEvents with payload {logsEventPayload}");

                    var date = DateTime.Now;
                    string key = $"{_bucketKeyPrefeix}/{_functionName}/{date.ToString("yyyy-MM")}/{Guid.NewGuid().ToString("N").ToUpper()}";

                    var request = new PutObjectRequest
                    {
                        BucketName = _bucketName,
                        ContentBody = logsEventPayload,
                        Key = key
                    };

                    var response = await _s3Client.PutObjectAsync(request);

                    // using var stream = new MemoryStream(Encoding.UTF8.GetBytes(logsEventPayload));

                    
                    // Firehose
                    // var record = new Record 
                    // {
                    //     Data = stream
                    // };
                    // var kinesisPutResponse = await _kinesisFirehoseClient.PutRecordAsync("logs-s3", record);


                    // Kinesis Data Streams
                    // var response = await _kinesisClient.PutRecordAsync(new Amazon.Kinesis.Model.PutRecordRequest
                    // {
                    //     Data = stream,
                    //     StreamName = "logs-s3",
                    //     PartitionKey = Guid.NewGuid().ToString()
                    // });

                    // _logger.LogInformation($"logged to shard {response.ShardId}");
                    
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error executing LogsCallback");
                }
            }
        }
    }
}