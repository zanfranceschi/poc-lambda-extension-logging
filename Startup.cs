using System.Collections.Concurrent;
using Amazon.S3;
using Amazon.KinesisFirehose;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Amazon.Kinesis;

namespace Poc.LambdaExtension.Logging
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            ConcurrentQueue<string> logsEventQueue = new ConcurrentQueue<string>();

            services.AddSingleton<ConcurrentQueue<string>>(logsEventQueue);
            
            services.AddControllers();
            services.AddAWSService<IAmazonS3>();
            services.AddAWSService<IAmazonKinesisFirehose>();
            services.AddAWSService<IAmazonKinesis>();
            
            services.AddHttpClient<ExtensionClient>();
            services.AddHttpClient<LoggingApiClient>();
            
            services.AddSingleton<ExtensionClient>();
            services.AddSingleton<LoggingApiClient>();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment()) { }

            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
