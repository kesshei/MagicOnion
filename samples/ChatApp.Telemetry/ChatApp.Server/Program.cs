using Grpc.Core;
using MagicOnion.Hosting;
using MagicOnion.OpenTelemetry;
using MagicOnion.Server;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Exporter.Prometheus;
using OpenTelemetry.Exporter.Zipkin;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using System;
using System.Threading.Tasks;

namespace ChatApp.Server
{
    class Program
    {
        static async Task Main(string[] args)
        {
            //GrpcEnvironment.SetLogger(new Grpc.Core.Logging.ConsoleLogger());
            
            await MagicOnionHost.CreateDefaultBuilder()
                .UseMagicOnion()
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddMagicOnionOpenTelemetry((options, meterOptions) =>
                    {
                        // open-telemetry with Prometheus exporter
                        meterOptions.MetricExporter = new PrometheusExporter(new PrometheusExporterOptions() { Url = options.MetricsExporterEndpoint });
                    },
                    (options, provider, tracerBuilder) =>
                    {
                        // open-telemetry with Zipkin exporter
                        tracerBuilder.AddZipkinExporter(o =>
                        {
                            o.ServiceName = "ChatApp.Server";
                            o.Endpoint = new Uri(options.TracerExporterEndpoint);
                        });
                        // ConsoleExporter will show current tracer activity
                        tracerBuilder.AddConsoleExporter();
                    });
                    services.AddHostedService<PrometheusExporterMetricsService>();
                })
                .ConfigureServices((hostContext, services) =>
                {
                    var meterProvider = services.BuildServiceProvider().GetService<MeterProvider>();
                    services.Configure<MagicOnionHostingOptions>(options =>
                    {
                        options.Service.GlobalFilters.Add(new OpenTelemetryCollectorFilterFactoryAttribute());
                        options.Service.GlobalStreamingHubFilters.Add(new OpenTelemetryHubCollectorFilterFactoryAttribute());
                        options.Service.MagicOnionLogger = new OpenTelemetryCollectorLogger(meterProvider, version: "0.5.0-beta.2");
                    });
                })
                .RunConsoleAsync();
        }
    }
}
