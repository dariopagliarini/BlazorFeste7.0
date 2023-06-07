namespace BlazorFeste.Services
{
  public class ClockTimerService : BackgroundService, IDisposable
  {
    private CancellationToken cancellationToken;
    private const int _runTime = 900;
    private readonly UserInterfaceService _userInterfaceService;
    public ClockTimerService(UserInterfaceService userInterfaceService)
    {
      _userInterfaceService = userInterfaceService;
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
      cancellationToken = stoppingToken;
      while (!cancellationToken.IsCancellationRequested)
      {
        _userInterfaceService.OnDataOraServer(DateTime.Now);
        await Task.Delay(TimeSpan.FromMilliseconds(_runTime), stoppingToken);
      }
    }
    public override void Dispose()
    {
      CancellationTokenSource source = new();
      cancellationToken = source.Token;
      source.Cancel();
    }

  }
}
