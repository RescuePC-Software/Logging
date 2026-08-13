using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Enrichers.Span;
using Serilog.Formatting.Json;

namespace RescuePC.Software.Logging.Providers.Serilog;

public static class SerilogExtensions
{
    public static void AddSerilog(this IHostBuilder host)
    {
        host.UseSerilog((context, services, configuration) =>
        {
            configuration
                .ReadFrom.Configuration(context.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext()
                .Enrich.WithSpan();

            if (context.HostingEnvironment.IsDevelopment())
            {
                configuration.WriteTo.Console();
            }
            else
            {
                configuration.WriteTo.Console(new JsonFormatter(renderMessage: true));
            }

            if (context.Configuration.GetValue<bool>("Serilog:FileLogging"))
            {
                configuration.WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day);
            }
        });
    }
}
