using System;
using System.Collections.Generic;
using System.Text.Json;

namespace Poc.LambdaExtension.Logging
{
    public class LogModel
    {
        public DateTime Time { get; set; }
        public string Type { get; set; } 
        public string Record { get; set; }

    }

    public static class LogModelExtensions
    {
        public static string Serialize(this IEnumerable<LogModel> list)
        {
            return JsonSerializer.Serialize(list);
        }
    }
}