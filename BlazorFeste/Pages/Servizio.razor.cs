using Blazored.Toast.Services;

using BlazorFeste.Components;
using BlazorFeste.DataAccess;
using BlazorFeste.Services;
using BlazorFeste.Util;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Newtonsoft.Json.Linq;

using System.Globalization;
using System.Net;

using System.Text;

namespace BlazorFeste.Pages
{
  public partial class Servizio : IDisposable
  {
    #region Inject
    [Inject] public NavigationManager _NavigationManager { get; init; }
    [Inject] public UserInterfaceService _UserInterfaceService { get; init; }
    [Inject] public IJSRuntime JSRuntime { get; init; }
    #endregion

    #region Variabili
    private IJSObjectReference Module;
    private Task<IJSObjectReference> _jsModule;
    private Task<IJSObjectReference> JsModule => _jsModule ??= JSRuntime.InvokeAsync<IJSObjectReference>("import", "./js/ServizioObj.js").AsTask();

    private DotNetObjectReference<Servizio> objRef;

    private bool tab1 = true;
    private bool tab2 = false;
    private bool tab3 = false;
    #endregion

    #region Metodi
    public void DisplayTab(int TabNumber)
    {
      switch (TabNumber)
      {
        case 1:
          this.tab1 = true;
          this.tab2 = false;
          this.tab3 = false;
          break;

        case 2:
          this.tab1 = false;
          this.tab2 = true;
          this.tab3 = false;
          break;

        case 3:
          this.tab1 = false;
          this.tab2 = false; 
          this.tab3 = true;
          break;

        default:
          break;
      }

    }

    private async Task<string> HttpRequestToCloud_ProductUpdate<T>(string _ClientIPAddress = "https://ghqb.galileicrema.org/brolo/api", int _TimeOutMSec = 5000)
    {
      HttpResponseMessage result = new HttpResponseMessage();

      HttpClient httpClient = new HttpClient();
      httpClient.Timeout = TimeSpan.FromMilliseconds(_TimeOutMSec);
      httpClient.DefaultRequestHeaders.Clear();
      httpClient.DefaultRequestHeaders.Add("key", "b0ZWHe8M+Whk8RO24cJiL4CQPKM");

      var cts = new CancellationTokenSource();
      string jsonResponse2 = string.Empty;

      try
      {
        // Aggiornamento Stato Prodotti
        var aaa = new StringBuilder();
        foreach (var item in _UserInterfaceService.AnagrProdotti.Values)
        {
          aaa.Append($"[{item.IdProdotto}, {Convert.ToInt32(item.Stato)}],");
        }
        var strProdotti = $"[{aaa.ToString().Remove(aaa.ToString().Length - 1)}]";

        var values2 = new Dictionary<string, string>
        {
          {"maga", strProdotti } // "[[1,0] , [ 2, 0]]" }  // IdProdotto, Stato
        };
        var content2 = new FormUrlEncodedContent(values2);
        using HttpResponseMessage response2 = await httpClient.PostAsync($"{_ClientIPAddress}/import.php", content2);
        jsonResponse2 = await response2.Content.ReadAsStringAsync();

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
      return (jsonResponse2);
    }
    private async Task<string> HttpRequestToCloud_ProductUpdateFull<T>(string _ClientIPAddress = "https://ghqb.galileicrema.org/brolo/api", int _TimeOutMSec = 5000)
    {
      HttpResponseMessage result = new HttpResponseMessage();

      HttpClient httpClient = new HttpClient();
      httpClient.Timeout = TimeSpan.FromMilliseconds(_TimeOutMSec);
      httpClient.DefaultRequestHeaders.Clear();
      httpClient.DefaultRequestHeaders.Add("key", "b0ZWHe8M+Whk8RO24cJiL4CQPKM");

      var cts = new CancellationTokenSource();
      string jsonResponse2 = string.Empty;

      // Gets a NumberFormatInfo associated with the en-US culture.
      NumberFormatInfo nfi = new CultureInfo("it-IT", false).NumberFormat;
      nfi.NumberDecimalSeparator = ".";

      try
      {
        // Aggiornamento Stato Prodotti
        var aaa = new StringBuilder();
        foreach (var item in _UserInterfaceService.AnagrProdotti.Values) 
        {//{item.IdListino}, 
          aaa.Append($"[{item.IdProdotto},\"{item.NomeProdotto.CR_to_Space().Replace("\"", "\\\"")}\",{Convert.ToDecimal(item.PrezzoUnitario).ToString("0.00", nfi)},{item.Magazzino},{Convert.ToInt32(item.Stato)},{item.IdLista},{item.IdMenu}],");
        }
        var strProdotti = $"[{aaa.ToString().Remove(aaa.ToString().Length - 1)}]";

        /*
        JSON per import completo ( /brolo/api/import.php ) : chiamata POST con parametri 
          cmd “full”
          maga array di [ IdListino, IdProdotto, NomeProdotto, PrezzoUnitario, Magazzino, Stato, IdLista, IdMenu ] 
        Viene realizzata una UPSERT (INSERT … ON DUPLICATE KEY UPDATE) 

        Esempio 
          cmd:full 
          maga:[[1,"CASONCELLI",6.0,500,1,1,1],[71,"GELATO PALLINA",2.5,500,0,9,5]] 
         */
        var values2 = new Dictionary<string, string>
        {
          {"cmd", "full"},
          {"maga", strProdotti } // array di [ IdListino, IdProdotto, NomeProdotto, PrezzoUnitario, Magazzino, Stato, IdLista, IdMenu ] 
        };
        var content = new FormUrlEncodedContent(values2);
        using HttpResponseMessage response = await httpClient.PostAsync($"{_ClientIPAddress}/import.php", content);
        jsonResponse2 = await response.Content.ReadAsStringAsync();

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
      return (jsonResponse2);
    }

    #endregion

    #region LifeCycle
    protected override Task OnInitializedAsync()
    {
      return base.OnInitializedAsync();
    }
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
      if (firstRender)
      {
        objRef = DotNetObjectReference.Create(this);
        Module = (await JsModule);

        await Module.InvokeVoidAsync("ServizioObj.init", objRef, _UserInterfaceService.AnagrCasse);
      }
      else
      {
        if (Module != null)
        {
        }
        else
        {
          Module = (await JsModule);
          await Module.InvokeVoidAsync("ServizioObj.init", objRef, _UserInterfaceService.AnagrCasse);
        }
      }
      await base.OnAfterRenderAsync(firstRender);
    }
    public void Dispose()
    {
      _jsModule?.Result.InvokeVoidAsync("ServizioObj.dispose");
      objRef?.Dispose();
    }
    #endregion

    #region JSInvokable
    [JSInvokable("NavigateToPage")]
    public void NavigateToPage(string navigateToPage)
    {
      _NavigationManager.NavigateTo($"/{navigateToPage}");
    }
    [JSInvokable("RefreshAnagrafiche")]
    public async Task<string> RefreshAnagrafiche(int iTipoAggiornamento)
    {
      string jsonResponse = string.Empty;
      HttpResponseMessage result = new HttpResponseMessage();
      string ClientIPAddress = "https://ghqb.galileicrema.org/brolo/api";

      switch (iTipoAggiornamento)
      {
        case 0: // Reload Anagrafiche
          _UserInterfaceService.DtFestaInCorso = DateTime.MinValue;
          break;

        case 1: // Aggiorna Stato Prodotti Cloud
          try
          {
            jsonResponse = await HttpRequestToCloud_ProductUpdate<string>(ClientIPAddress);
          }
          catch (Exception ex)
          {
          }
          break;

        case 2: // Aggiorna Anagrafica Prodotti Cloud
          try
          {
            jsonResponse = await HttpRequestToCloud_ProductUpdateFull<string>(ClientIPAddress);
          }
          catch (Exception ex)
          {
          }
          break;

        default:
          break;
      }
      return (jsonResponse);
    }
    #endregion
  }
}
