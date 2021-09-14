using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Poc.LambdaExtension.Logging.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class LoggingController : ControllerBase
    {
        private readonly ILogger<LoggingController> _logger;

        public LoggingController(ILogger<LoggingController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Get() 
        {
            _logger.LogInformation("get");
            return Ok(new { message = "get" });
        }

        [HttpPost]
        public IActionResult Post(object payload) 
        {
            _logger.LogInformation("post");
            return Ok(new { message = "post" });
        }

        [HttpPut]
        public IActionResult Put(object payload) 
        {
            _logger.LogInformation("put");
            return Ok(new { message = "put" });
        }
    }
}
