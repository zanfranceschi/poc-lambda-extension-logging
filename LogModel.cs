using System;

namespace Poc.LambdaExtension.Logging
{
    public class LogModel
    {
        public DateTime Time { get; set; }
        public string Type { get; set; } 
        public object Record { get; set; }

    }
}