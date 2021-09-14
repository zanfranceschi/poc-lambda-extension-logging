using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Poc.LambdaExtension.Logging.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class LoggingController : ControllerBase
    {
        private readonly ILogger<LoggingController> _logger;
        private readonly IAmazonS3 _s3Client;
        private readonly string _bucketName;
        private readonly string _bucketKeyPrefeix;
        private readonly string _functionName;

        public LoggingController(
            ILogger<LoggingController> logger,
            IAmazonS3 s3Client)
        {
            _logger = logger;
            _s3Client = s3Client;
            _bucketName = "zanfranceschi";
            _bucketKeyPrefeix = "logs";
            _functionName = Environment.GetEnvironmentVariable("AWS_LAMBDA_FUNCTION_NAME") ?? "sem-nome";
        }

        [HttpGet]
        public IActionResult Get()
        {
            _logger.LogInformation("get");
            return Ok(new { message = "get" });
        }

        [HttpPost]
        public async Task<IActionResult> Post(IEnumerable<LogModel> logs)
        {
            DateTime date = logs.Where(l => l.Type == "function").Select(l => l.Time).Min();

            string key = $"{_bucketKeyPrefeix}/{_functionName}/{date.Year}/{date.Month.ToString().PadLeft(2, '0')}/{Guid.NewGuid().ToString("N")}";
            
            string logFileContent = logs.Where(l => l.Type == "function").Serialize();
            
            var request = new PutObjectRequest
            {
                BucketName = _bucketName,
                ContentBody = logFileContent,
                Key = key
            };
            
            var response = await _s3Client.PutObjectAsync(request);

            _logger.LogInformation($"response.HttpStatusCode: {response.HttpStatusCode}");
            
            return Ok($"ok");
        }
    }
}
