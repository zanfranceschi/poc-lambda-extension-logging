using System;
using System.Collections.Concurrent;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Poc.LambdaExtension.Logging.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class LoggingController : ControllerBase
    {
        private readonly ILogger<LoggingController> _logger;

        private readonly ConcurrentQueue<string> _logsQueue;

        public LoggingController(
            ILogger<LoggingController> logger,
            ConcurrentQueue<string> logsQueue)
        {
            _logger = logger;
            _logsQueue = logsQueue;
        }

        [HttpGet]
        public IActionResult Get()
        {
            _logger.LogInformation("get");
            return Ok(new { message = "get" });
        }

        [HttpPost]
        public IActionResult LogsCallback(object payload)
        {
            try
            {
                _logsQueue.Enqueue(payload.ToString());
                return Ok(new { message = "ok" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error@LogsCallback");
                throw;
            }
        }
    }
}
