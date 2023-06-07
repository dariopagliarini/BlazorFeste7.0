using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace PrinterServerAPI
{
  public class Program
  {
    public static void Main(string[] args)
    {
      Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .Enrich.FromLogContext()
            .WriteTo.Console(
              outputTemplate: "{Timestamp:dd/MM/yyyy HH:mm:ss.fff} [{Level:u3}] {Message}{NewLine}{Exception}"
            )
            .WriteTo.File(
              "logs/PrinterServerAPI.log",
              rollingInterval: RollingInterval.Day)
            .CreateLogger();

      Log.Information("Starting PrinterServer API Service");
      CreateHostBuilder(args).Build().Run();
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(webBuilder =>
            {
              webBuilder.UseStartup<Startup>();
            });
  }
}
