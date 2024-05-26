using Blazored.Toast;

using BlazorFeste.DataAccess;
using BlazorFeste.Services;
using BlazorFeste.Services.AppOrdini;

using Serilog;

using System.Globalization;

var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .Build();

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(configuration)
    .CreateLogger();

var section = configuration.GetSection("Serilog:Properties:ApplicationName");

var applicationName = section != null ? section.Value : "unknow";

try
{
  Log.Information("---------------------------------------------------");
  Log.Information($"       Application {applicationName} started");
  Log.Information("---------------------------------------------------");

  var builder = WebApplication.CreateBuilder(args);

  builder.Host.UseSerilog((ctx, lc) => lc
               .ReadFrom.Configuration(configuration));

  // Add services to the container.
  builder.Services.AddRazorPages();
  builder.Services.AddServerSideBlazor(options => options.JSInteropDefaultCallTimeout = TimeSpan.FromSeconds(30))
        .AddHubOptions(options =>
        {
          options.ClientTimeoutInterval = TimeSpan.FromSeconds(60);
          options.EnableDetailedErrors = false;
          options.HandshakeTimeout = TimeSpan.FromSeconds(30);
          options.KeepAliveInterval = TimeSpan.FromSeconds(15);
          options.MaximumParallelInvocationsPerClient = 1;
          options.MaximumReceiveMessageSize = 128 * 1024 * 1024; // 128 MB
          options.StreamBufferCapacity = 10;
        });

  builder.Services.AddHttpContextAccessor();
  builder.Services.AddLocalization();
  builder.Services.AddBlazoredToast();

  builder.Services.AddSingleton<FesteDataAccess>()
                  .AddSingleton<UserInterfaceService>()
                  .AddSingleton<IAppOrdiniService, AppOrdiniService>()

                  .AddHostedService<ClockTimerService>()
                  .AddHostedService<DatabaseTimerService>()
                  .AddHostedService<AppOrdiniHandlerService>()

                  .AddScoped<ClientInformationService>();

  var app = builder.Build();

  app.UseStaticFiles();
  app.UseRouting();
  app.UseRequestLocalization("it-IT");  
  CultureInfo.CurrentCulture = new CultureInfo("it-IT");

  app.MapBlazorHub();
  app.MapFallbackToPage("/_Host");

  app.Run();
}
catch (Exception ex)
{
  Log.Fatal(ex, $"Application {applicationName} failed to start");
}
finally
{
  Log.Information("Shut down complete");
  Log.CloseAndFlush();
}
