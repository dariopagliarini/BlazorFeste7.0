using Blazored.Toast.Services;

using BlazorFeste.Classes;
using BlazorFeste.Data.Models;
using BlazorFeste.DataAccess;
using BlazorFeste.lib;
using BlazorFeste.Services;
using BlazorFeste.Util;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

using Newtonsoft.Json.Serialization;

using Serilog;

using System.Net;

namespace BlazorFeste.Components
{
  public partial class GestioneAnagrListe : IDisposable
  {
    #region Inject
    [Inject] public ClientInformationService clientInfo { get; init; }
    [Inject] public IToastService toastService { get; init; }
    [Inject] public UserInterfaceService _UserInterfaceService { get; init; }
    [Inject] public FesteDataAccess festeDataAccess { get; init; }
    [Inject] public IJSRuntime JSRuntime { get; init; }
    #endregion

    #region Variabili
    private IJSObjectReference Module;
    private Task<IJSObjectReference> _jsModule;
    private Task<IJSObjectReference> JsModule => _jsModule ??= JSRuntime.InvokeAsync<IJSObjectReference>("import", "./js/GestioneAnagrListeObj.js").AsTask();

    private DotNetObjectReference<GestioneAnagrListe> objRef;

    List<StatoCasse> _qryStatoCasse = new List<StatoCasse>();
    List<StatoOrdini> _qryStatoOrdini = new List<StatoOrdini>();
    List<StatoListe> _qryStatoListe = new List<StatoListe>();

    string JSON_qryStatoOrdini = string.Empty;

    string strElapsed = string.Empty;

    private CamelCasePropertyNamesContractResolver contractResolver = new CamelCasePropertyNamesContractResolver { NamingStrategy = new CamelCaseNamingStrategy { } };
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

        var AnagrListe = (await festeDataAccess.GetGenericQuery<AnagrListe>("SELECT * FROM anagr_liste WHERE IdListino = @IdListino ORDER BY IdLista ",
          new { IdListino = _UserInterfaceService.ArchFesta.IdListino })).ToList();

        Module = (await JsModule);
        await Module.InvokeVoidAsync("GestioneAnagrListeObj.init", objRef, AnagrListe);
      }
      else
      {
        if (Module != null)
        {
        }
        else
        {
          var AnagrListe = (await festeDataAccess.GetGenericQuery<AnagrListe>("SELECT * FROM anagr_liste WHERE IdListino = @IdListino ORDER BY IdLista ",
            new { IdListino = _UserInterfaceService.ArchFesta.IdListino })).ToList();

          Module = (await JsModule);
          await Module.InvokeVoidAsync("GestioneAnagrListeObj.init", objRef, AnagrListe);
        }
      }
      await base.OnAfterRenderAsync(firstRender);
    }
    public void Dispose()
    {
      //_jsModule?.Result.InvokeVoidAsync("DashBoardObj.dispose");
      objRef?.Dispose();
    }
    #endregion

    #region Eventi
    #endregion

    #region Metodi
    private async Task<HttpResponseMessage> RichiediStampaConsumi(AnagrListe _lista, int _idStampante)
    {
      HttpResponseMessage result = new HttpResponseMessage();
      StampaOrdine_ConsumiGiornata consumiGiornata;

      try
      {
        // Recupero i dati della festa dal Database (la serata in corso) con righe della lista che devo filtrare
        var DatiFestaInCorso = from o in _UserInterfaceService.QryOrdini.Where(w => w.Value.DataAssegnazione == _UserInterfaceService.DtFestaInCorso)
                               join r in _UserInterfaceService.QryOrdiniRighe.Where(w => w.Value.IdCategoria == _lista.IdLista) on o.Key equals r.Key.Item1
                               select r;

        // Se ho qualche ordine già fatto, recupero i dati 
        if (DatiFestaInCorso.Any())
        {
          var _qryStatoProdotti = (from r in DatiFestaInCorso
                                   group r by r.Value.IdProdotto into g
                                   orderby g.Key
                                   select new
                                   {
                                     IdProdotto = g.Key,
                                     Importo = g.Sum(s => s.Value.Importo),
                                     QuantitàProdotto = g.Sum(s => s.Value.QuantitàProdotto),
                                   });

          consumiGiornata = new StampaOrdine_ConsumiGiornata
          {
            IPAddress = clientInfo.IPAddress,
            strNomeGiornata = $"{_UserInterfaceService.DtFestaInCorso.ToString("ddd dd/MM").ToUpper()} - {(_UserInterfaceService.DtFestaInCorso.Hour == 12 ? "PRANZO" : "CENA")}",
            Festa = _UserInterfaceService.ArchFesta,
            Lista = _lista,
            statoLista = (from p in _UserInterfaceService.AnagrProdotti.Values
                          join r in _qryStatoProdotti on p.IdProdotto equals r.IdProdotto
                          orderby p.IdProdotto
                          select new StampaOrdine_StatoLista
                          {
                            IdProdotto = p.IdProdotto,
                            NomeProdotto = p.NomeProdotto.CR_to_Space(),
                            Importo = r.Importo,
                            Quantità = r.QuantitàProdotto,
                          }).ToList()
          };

          // Recupero le informazioni relative alla stampante
          var _stampante = _UserInterfaceService.AnagrStampanti.Where(w => w.IdStampante == _idStampante).FirstOrDefault();

          // Locale o Remota deve essere interpretato dal punto di vista della Cassa (cioè del Client)
          string ClientIPAddress = string.Empty;
          if (_stampante.IsRemote == true)
          {
            // Se la stampante è Remota recupero l'indirizzo nel campo RemoteAddress
            ClientIPAddress = _stampante.RemoteAddress;
          }
          else
          {
            // Se la stampante è Locale significa che è attaccata al PC Client da cui stò compilando l'ordine
            // quindi la devo cercare sull'indirizzo del client
            ClientIPAddress = clientInfo.IPAddress;
          }

          // La stampa è gestita dal programma "PrinterServerAPI" che deve essere in esecuzione
          // su ogni PC a cui è attaccata almeno una delle stampanti termiche che stampano gli scontrini.
          var scontrino = new GestioneScontrini_Ordini();
          StampaOrdine_RawData stampaOrdineRawData = new StampaOrdine_RawData
          {
            IPAddress = ClientIPAddress,
            Stampante = _stampante.SerialPort,
            DebugText = $"{_lista.Lista} - Stampa Consumi",
            LogEnabled = true,
            rawData = scontrino.Prepara_Consumi_Lista(consumiGiornata)
          };
          await HttpRequestToPrinterController<StampaOrdine_RawData>(ClientIPAddress, "rawData", stampaOrdineRawData);
        }
      }
      catch (Exception ex)
      {
        toastService.ShowError(ex.Message);
        Log.Error(ex, $"{clientInfo.IPAddress} - Code Exception");
      }
      return (result);
    }

    private async Task<HttpResponseMessage> HttpRequestToPrinterController<T>(string _ClientIPAddress, string _ControllerRoute, T _DatiDaInviare, int _TimeOutMSec = 5000)
    {
      HttpResponseMessage result = new HttpResponseMessage();

      HttpClient httpClient = new HttpClient();
      httpClient.Timeout = TimeSpan.FromMilliseconds(_TimeOutMSec);

      var cts = new CancellationTokenSource();
      try
      {
        result = await httpClient.PostAsJsonAsync<T>($"http://{_ClientIPAddress}:5000/api/StampaOrdine/{_ControllerRoute}", _DatiDaInviare, cts.Token);
        switch (result.StatusCode)
        {
          case HttpStatusCode.NotFound:
            toastService.ShowInfo($"HttpRequestToPrinterController - Verificare la stampante"); // "Errore Stampante"
            break;

          default:
            break;
        }
      }
      catch (WebException ex)
      {
        // handle web exception
        toastService.ShowError($"{ex.Message}"); // "Errore Stampante"
        Log.Error(ex, $"{clientInfo.IPAddress} - HttpRequestToPrinterController 1");
      }
      catch (TaskCanceledException ex)
      {
        if (ex.CancellationToken == cts.Token)
        {
          // a real cancellation, triggered by the caller
          toastService.ShowError($"{ex.Message}"); // "Errore Stampante"
          Log.Error(ex, $"{clientInfo.IPAddress} - HttpRequestToPrinterController 2");
        }
        else
        {
          // a web request timeout (possibly other things!?)
          toastService.ShowError($"Problemi Accesso PrinterServer - {_ClientIPAddress}"); // "Errore Stampante"
          Log.Error(ex, $"{clientInfo.IPAddress} - HttpRequestToPrinterController 3");
        }
      }
      return (result);
    }
    #endregion

    #region JSInvokable
    [JSInvokable("BatchUpdateRequest")]
    public async Task<List<AnagrListe>> BatchUpdateRequest(List<AnagrDataChange> changes)
    {
      foreach (var change in changes)
      {
        if (change.Type == "update")
        {
          await festeDataAccess.UpdateAnagrListeAsync(change);
        }

        if (change.Type == "insert")
        {
          await festeDataAccess.InsertAnagrListeAsync(change);
        }
      }
      // Aggiorna la variabile globale
      _UserInterfaceService.AnagrListe = (await festeDataAccess.GetGenericQuery<AnagrListe>("SELECT * FROM anagr_liste WHERE Abilitata <> 0 AND IdListino = @IdListino ORDER BY IdLista ",
        new { _UserInterfaceService.ArchFesta.IdListino })).ToList();

      _UserInterfaceService.OnNotifyAnagrListe(false);

      var NewAnagrListe = (await festeDataAccess.GetGenericQuery<AnagrListe>("SELECT * FROM anagr_liste WHERE IdListino = @IdListino ORDER BY IdLista ",
          new { _UserInterfaceService.ArchFesta.IdListino })).ToList();

      return (NewAnagrListe);
    }

    [JSInvokable("OnPrintRequest_Consumi")]
    public async Task OnPrintRequest_Consumi()
    {
      List<int> StatiLista = new List<int>();
      StatiLista = Enumerable.Range(1, 9).ToList();

      try
      {
        foreach (var Lista in _UserInterfaceService.AnagrListe.Where(w => w.Abilitata.Value && StatiLista.Contains(w.IdLista)))
        {
          HttpResponseMessage result = (await RichiediStampaConsumi(Lista, 1));

          switch (result.StatusCode)
          {
            case HttpStatusCode.NotFound:
              toastService.ShowInfo($"Errore Stampa Consumi");

              break;
            default:
              break;
          }
          System.Threading.Thread.Sleep(400);
        }
      }
      catch (Exception ex)
      {
        toastService.ShowError(ex.Message);
        Log.Error(ex, $"{clientInfo.IPAddress} - Code Exception");
        //_ = ex;
        //Console.WriteLine("I/O error");
        //Log.Fatal(ex, "I/O error:");
      }
    }

    [JSInvokable("OnPrintRequest_ConsumiLista")]
    public async Task OnPrintRequest_ConsumiLista(AnagrListe _lista)
    {
      try
      {
        HttpResponseMessage result = (await RichiediStampaConsumi(_lista, 1));

        switch (result.StatusCode)
        {
          case HttpStatusCode.NotFound:
            toastService.ShowInfo($"Errore Stampa Consumi");

            break;
          default:
            break;
        }
      }
      catch (Exception ex)
      {
        toastService.ShowError(ex.Message);
        Log.Error(ex, $"{clientInfo.IPAddress} - Code Exception");
        //_ = ex;
        //Console.WriteLine("I/O error");
        //Log.Fatal(ex, "I/O error:");
      }
    }


    #endregion

  }
}
