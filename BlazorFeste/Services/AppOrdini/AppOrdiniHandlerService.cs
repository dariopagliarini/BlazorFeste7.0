using System.Net;

namespace BlazorFeste.Services.AppOrdini
{
  public class AppOrdiniHandlerService(ILogger<AppOrdiniHandlerService> logger, IAppOrdiniService appOrdiniState, UserInterfaceService userInterfaceService) : BackgroundService
  {
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
      //      appOrdini = JsonConvert.DeserializeObject<List<AppListaOrdini>>(aaa);
      //System.Threading.Timer Order_Timer = new System.Threading
      //  .Timer(new TimerCallback(CaricaListaOrdiniAsync), null, 5000, 10000);

      while (!stoppingToken.IsCancellationRequested)
      {
        if (userInterfaceService != null)
        {
          if (userInterfaceService.ArchFesta != null)
          {
            if (userInterfaceService.ArchFesta.WebAppAttiva)
            {
              if (appOrdiniState.HasOrdiniDaEvadereReady)
              {
                var ordineDaEvadere = appOrdiniState.GetOrdiniDaEvadere();
                try
                {
                  await HttpRequestToCloud_EvadiOrdineAsync<string>("https://ghqb.galileicrema.org/brolo/api", ordineDaEvadere);
//                  CaricaListaOrdiniAsync(null);
                  //            logger.LogInformation($"AppOrdini - Evasione ordine {ordineDaEvadere} OK");
                }
                catch (Exception ex)
                {
                  logger.LogError(ex, $"AppOrdini - Errore evasione ordine {ordineDaEvadere}");
                }
              }
            }
          }
        }
        await Task.Delay(TimeSpan.FromMilliseconds(250), stoppingToken);
      }
    }
    private async void CaricaListaOrdiniAsync(object state)
    {
      if (userInterfaceService != null)
      {
        if (userInterfaceService.ArchFesta != null)
        {
          if (userInterfaceService.ArchFesta.WebAppAttiva)
          {
            var _appListaOrdini = await HttpRequestToCloud_Order<string>("https://ghqb.galileicrema.org/brolo/api", 0); // -34400
            appOrdiniState.UpdateListaOrdini(_appListaOrdini);

            //logger.LogInformation($"AppOrdini - CaricaListaOrdiniAsync {_appListaOrdini.Length} bytes");
          }
        }
      }
    }
    private async Task<string> HttpRequestToCloud_Order<T>(string _ClientIPAddress, long _idOrdine, int _TimeOutMSec = 5000)
    {
      HttpResponseMessage result = new HttpResponseMessage();

      HttpClient httpClient = new HttpClient();
      httpClient.Timeout = TimeSpan.FromMilliseconds(_TimeOutMSec);
      httpClient.DefaultRequestHeaders.Clear();
      httpClient.DefaultRequestHeaders.Add("key", "b0ZWHe8M+Whk8RO24cJiL4CQPKM");

      string jsonResponse = string.Empty;

      var cts = new CancellationTokenSource();
      try
      {
        // Lettura Dati Ordine
        var values = new Dictionary<string, string>
        {
          {"idO", $"{_idOrdine}" }
        };
        var content = new FormUrlEncodedContent(values);
        using HttpResponseMessage response = await httpClient.PostAsync($"{_ClientIPAddress}/export.php", content);
        jsonResponse = await response.Content.ReadAsStringAsync();

        result = response;
        switch (result.StatusCode)
        {
          case HttpStatusCode.NotFound:
            //toastService.ShowInfo($"HttpRequestToCloud_Order - Errore Accesso Web {_ClientIPAddress}"); // "Errore Stampante"
            logger.LogInformation($"{_ClientIPAddress} - HttpRequestToCloud_Order - WebException");
            break;

          default:
            break;
        }
      }
      catch (WebException ex)
      {
        // handle web exception
        //toastService.ShowError($"{ex.Message}"); // "Errore Stampante"
        logger.LogError(ex, $"{_ClientIPAddress} - HttpRequestToCloud_Order - WebException");
      }
      catch (TaskCanceledException ex)
      {
        if (ex.CancellationToken == cts.Token)
        {
          // a real cancellation, triggered by the caller
          //toastService.ShowError($"{ex.Message}"); // "Errore Stampante"
          logger.LogError(ex, $"HttpRequestToCloud_Order - TaskCanceledException");
        }
        else
        {
          // a web request timeout (possibly other things!?)
          //toastService.ShowError($"Problemi Accesso PrinterServer - {_ClientIPAddress}"); // "Errore Stampante"
          //Log.Error(ex, $"{clientInfo.IPAddress} - HttpRequestToPrinterController 3");
        }
      }
      return (jsonResponse);
    }

    private async Task<string> HttpRequestToCloud_EvadiOrdineAsync<T>(string _ClientIPAddress = "https://ghqb.galileicrema.org/brolo/api", long _idOrdine = 0, int _TimeOutMSec = 5000)
    {
      HttpResponseMessage result = new HttpResponseMessage();

      HttpClient httpClient = new HttpClient();
      httpClient.Timeout = TimeSpan.FromMilliseconds(_TimeOutMSec);
      httpClient.DefaultRequestHeaders.Clear();
      httpClient.DefaultRequestHeaders.Add("key", "b0ZWHe8M+Whk8RO24cJiL4CQPKM");

      var cts = new CancellationTokenSource();
      string jsonResponse = string.Empty;

      try
      {
        // Evasione dell'ordine
        var values = new Dictionary<string, string>
        {
          {"evadi", _idOrdine.ToString() }
        };
        var content = new FormUrlEncodedContent(values);
        using HttpResponseMessage response = await httpClient.PostAsync($"{_ClientIPAddress}/import.php", content);
        jsonResponse = await response.Content.ReadAsStringAsync();

        switch (result.StatusCode)
        {
          case HttpStatusCode.NotFound:
            //            toastService.ShowInfo($"HttpRequestToPrinterController - Verificare la stampante"); // "Errore Stampante"
            break;

          default:
            break;
        }
      }
      catch (WebException ex)
      {
        // handle web exception
        //        toastService.ShowError($"{ex.Message}"); // "Errore Stampante"
        //        Log.Error(ex, $"{clientInfo.IPAddress} - HttpRequestToPrinterController 1 - WebException");
      }
      catch (TaskCanceledException ex)
      {
        if (ex.CancellationToken == cts.Token)
        {
          // a real cancellation, triggered by the caller
          //          toastService.ShowError($"{ex.Message}"); // "Errore Stampante"
          //          Log.Error(ex, $"{clientInfo.IPAddress} - HttpRequestToPrinterController 2 - TaskCanceledException");
        }
        else
        {
          // a web request timeout (possibly other things!?)
          //          toastService.ShowError($"Problemi Accesso PrinterServer - {_ClientIPAddress}"); // "Errore Stampante"
          //Log.Error(ex, $"{clientInfo.IPAddress} - HttpRequestToPrinterController 3");
        }
      }
      return (jsonResponse);
    }
  }
}

