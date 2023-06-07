using Blazored.Toast.Services;

using BlazorFeste.Data.Models;
using BlazorFeste.DataAccess;
using BlazorFeste.lib;
using BlazorFeste.Services;
using BlazorFeste.Util;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

using Serilog;

using System.Net;

namespace BlazorFeste.Pages
{
  public partial class GestioneCassa : IDisposable
  {
    [Parameter]
    public int IdCassa { get; set; }

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
    private Task<IJSObjectReference> JsModule => _jsModule ??= JSRuntime.InvokeAsync<IJSObjectReference>("import", "./js/GestioneCassaObj.js").AsTask();

    private DotNetObjectReference<GestioneCassa> objRef;
    bool flagInizializza { get; set; } = false;

    AnagrCasse Cassa = new AnagrCasse();
    public string strTitolo { get; set; } = "Cassa Non Abilitata";
    List<AnagrProdotti> TabellaProdotti { get; set; }
    #endregion

    #region LifeCycle
    protected override Task OnInitializedAsync()
    {
      _UserInterfaceService.DataOraServer += OnDataOraServerChanged;
      _UserInterfaceService.NotifyStatoProdotti += OnNotifyStatoProdotti;
      _UserInterfaceService.NotifyAnagrProdotti += OnNotifyAnagrProdotti;

      return base.OnInitializedAsync();
    }
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
      if (firstRender)
      {
        objRef = DotNetObjectReference.Create(this);

        Module = (await JsModule);
      }

      if (flagInizializza)
      {
        flagInizializza = false;

        if (!(Cassa is null))
        {
          await Module.InvokeVoidAsync("GestioneCassaObj.createButtons", IdCassa, TabellaProdotti);
          await Module.InvokeVoidAsync("GestioneCassaObj.init", objRef, Cassa);

          //if (Cassa.PortName != "")  && Cassa.IsRemote == false)
          if (Cassa.ScontrinoAbilitato.Value)
            await RescanSerialPorts(Cassa, true);
        }
      }
      await base.OnAfterRenderAsync(firstRender);
    }
    protected override async Task OnParametersSetAsync()
    {
      AggiornaDatiCassa();

      flagInizializza = true;

      await base.OnParametersSetAsync();
    }

    public void Dispose()
    {
      _UserInterfaceService.DataOraServer -= OnDataOraServerChanged;
      _UserInterfaceService.NotifyStatoProdotti -= OnNotifyStatoProdotti;
      _UserInterfaceService.NotifyAnagrProdotti -= OnNotifyAnagrProdotti;

      _jsModule?.Result.InvokeVoidAsync("GestioneCassaObj.dispose");

      objRef?.Dispose();
    }
    #endregion

    #region JSInvokable
    [JSInvokable("OnSaveToMySQLAsync")]
    public async Task<int> OnSaveToMySQLAsync(bool _PrintEnabled, string _TipoOrdine, string _Tavolo, string _Coperti, string _NotaOrdine, string _Referente, List<RigaCassa> Righe)
    {
      ArchOrdini _archOrdine = new ArchOrdini
      {
        Cassa = IdCassa.ToString(),
        TipoOrdine = _TipoOrdine,
        Tavolo = _Tavolo,
        NumeroCoperti = _Coperti,
        NoteOrdine = _NotaOrdine,
        IdStatoOrdine = 0,
        Referente = _Referente,
        DataOra = DateTime.Now
      };
      List<ArchOrdiniRighe> _archOrdiniRighe = new List<ArchOrdiniRighe>();

      // Verifico se l'ordine ha qualche tipologia di sconto
      double _TotaleOrdine = Righe.Sum(s => (s.PrezzoUnitario * s.QuantitàProdotto));
      if (_TotaleOrdine < 0)
      {
        // Devo risistemare il valore del buono
        int _NumeroBuoni = Righe.Where(w => w.PrezzoUnitario < 0).Sum(s => s.QuantitàProdotto);

        foreach (var item in Righe.Where(w => w.PrezzoUnitario < 0))
        {
          item.PrezzoUnitario = item.PrezzoUnitario - (_TotaleOrdine / _NumeroBuoni);
        }
      }

      //_TotaleOrdine = Righe.Sum(s => (s.PrezzoUnitario * s.QuantitàProdotto));
      int iRiga = 1;
      foreach (var item in Righe.Where(w => w.QuantitàProdotto > 0))
      {
#if THREADSAFE
        var a = (from prod in _UserInterfaceService.AnagrProdotti.Values.Where(w => w.IdProdotto == item.IdProdotto)
#else
        var a = (from prod in _UserInterfaceService.AnagrProdotti.Where(w => w.IdProdotto == item.IdProdotto)
#endif
                 join lista in _UserInterfaceService.AnagrListe on prod.IdLista equals lista.IdLista
                 select new { prod, lista }).FirstOrDefault();

#if THREADSAFE
        AnagrProdotti _anagrProdotto = _UserInterfaceService.AnagrProdotti.Values.Where(w => w.IdProdotto == item.IdProdotto).FirstOrDefault();
#else
        AnagrProdotti _anagrProdotto = _UserInterfaceService.AnagrProdotti.Where(w => w.IdProdotto == item.IdProdotto).FirstOrDefault();
#endif
        _anagrProdotto.Consumo += Convert.ToUInt32(item.QuantitàProdotto);

        var _consumoCumulativo = _anagrProdotto.ConsumoCumulativo;
        if (_anagrProdotto.IdProdotto != _anagrProdotto.EvadiSuIdProdotto)
        {
          // Devo aggiornare anche il prodotto dove tengo il conteggio cumulativo
#if THREADSAFE
          AnagrProdotti _anagrProdottoSuCuiEvadere = _UserInterfaceService.AnagrProdotti.Values.Where(w => w.IdProdotto == _anagrProdotto.EvadiSuIdProdotto).FirstOrDefault();
#else
          AnagrProdotti _anagrProdottoSuCuiEvadere = _UserInterfaceService.AnagrProdotti.Where(w => w.IdProdotto == _anagrProdotto.EvadiSuIdProdotto).FirstOrDefault();
#endif
          _consumoCumulativo = _anagrProdottoSuCuiEvadere.ConsumoCumulativo;
          _anagrProdottoSuCuiEvadere.ConsumoCumulativo += Convert.ToUInt32(item.QuantitàProdotto);
          await festeDataAccess.UpdateAnagrProdottiAsync(_anagrProdottoSuCuiEvadere);
        }
        else
        {
          _anagrProdotto.ConsumoCumulativo += Convert.ToUInt32(item.QuantitàProdotto);
        }
        await festeDataAccess.UpdateAnagrProdottiAsync(_anagrProdotto);

        //if (_anagrProdotto.IdProdotto == 29)
        //  Log.Warning($"ArchOrdiniRighe Insert - {_anagrProdotto.IdProdotto} : Consumo: {_anagrProdotto.Consumo}/{_anagrProdotto.ConsumoCumulativo} - Evaso: {_anagrProdotto.Evaso}/{_anagrProdotto.EvasoCumulativo} - QueueTicket: {_consumoCumulativo + 1}");

        _archOrdiniRighe.Add(new ArchOrdiniRighe
        {
          IdOrdine = 0,
          IdRiga = iRiga++,
          IdCategoria = a.prod.IdLista,
          Categoria = a.lista.Lista,
          IdProdotto = item.IdProdotto,
          NomeProdotto = item.NomeProdotto,
          IdStatoRiga = 0,
          QuantitàProdotto = item.QuantitàProdotto,
          QuantitàEvasa = 0,
          Importo = item.PrezzoUnitario * item.QuantitàProdotto,
          DataOra_RigaPresaInCarico = default(DateTime),
          DataOra_RigaEvasa = default(DateTime),
          QueueTicket = _consumoCumulativo + 1
        });

        // Questa gestione è stata spostata nel servizio DatabaseTimerService
        //AnagrProdotti _anagrProdotto = _UserInterfaceService.AnagrProdotti.Where(w => w.IdProdotto == item.IdProdotto).FirstOrDefault();
        //_anagrProdotto.Consumo += Convert.ToUInt32(item.QuantitàProdotto);
        //await festeDataAccess.UpdateAnagrProdottiAsync(_anagrProdotto);
      }
      var idOrdine = await festeDataAccess.InsertArchOrdiniAsync(_archOrdine, _archOrdiniRighe);

      _UserInterfaceService.OnNotifyStatoProdotti(_archOrdine.IdCassa);

      toastService.ShowSuccess($"Ordine #{idOrdine} creato con successo"); // , "Avanti il prossimo"

      try
      {
        // Verifico se ho qualche riga da stampare anche in cucina (solo se Ordine== "SERVITO")
        var _RigheDaStampareInCucina = (from l in _UserInterfaceService.AnagrListe.Where(w => w.Cucina_StampaScontrino == true)
                                        join r in _archOrdiniRighe on l.IdLista equals r.IdCategoria
                                        select r.IdRiga).Any()
                                        &&
                                        (_TipoOrdine == "SERVITO");

        if (_PrintEnabled || _RigheDaStampareInCucina)
        {
          // Recupero i dati dell'ordine dal database (con tutti i valori aggiornati dai trigger)
          ArchOrdini _ordine = (await festeDataAccess.GetGenericQuery<ArchOrdini>($"SELECT * FROM arch_ordini WHERE IdOrdine = {idOrdine}")).ToList().FirstOrDefault();
          List<ArchOrdiniRighe> _ordineRighe = (await festeDataAccess.GetGenericQuery<ArchOrdiniRighe>($"SELECT r.* FROM arch_ordini o JOIN arch_ordini_righe r ON o.IdOrdine = r.IdOrdine WHERE o.IdOrdine = {idOrdine}")).ToList();

          if (_PrintEnabled)
          {
            HttpResponseMessage result = (await RichiediStampaScontrino(_ordine, _ordineRighe));
            switch (result.StatusCode)
            {
              case HttpStatusCode.NotFound:
                toastService.ShowInfo($"Errore Stampa Ordine {_archOrdine.DataOra.ToString("dd/MM/yyyy HH:mm:ss")}");

                break;
              default:
                break;
            }
          }

          if (_RigheDaStampareInCucina)
          {
            HttpResponseMessage result = (await RichiediStampaScontrinoCucina(_ordine, _ordineRighe));

            switch (result.StatusCode)
            {
              case HttpStatusCode.NotFound:
                toastService.ShowInfo($"Errore Stampa Ordine {_archOrdine.DataOra.ToString("dd/MM/yyyy HH:mm:ss")}");

                break;
              default:
                break;
            }
          }
        }
      }
      catch (Exception ex)
      {
        toastService.ShowError(ex.Message);
        Log.Error($"{clientInfo.IPAddress} - OnSaveToMySQLAsync - {ex.Message}");
        //_ = ex;
        //Console.WriteLine("I/O error");
        //Log.Fatal(ex, "I/O error:");
      }

      // Questa gestione è stata spostata nel servizio DatabaseTimerService
      //AggiornaDatiCassa();
      //await Module.InvokeVoidAsync("GestioneCassaObj.updateStatoProdotti", TabellaProdotti);

      return (idOrdine);
    }

    [JSInvokable("OnPrintRequest_StampaDiProva")]
    public async Task OnPrintRequest_StampaDiProva()
    {
      try
      {
        HttpResponseMessage result = (await RichiediStampaDiProva(Cassa));

        switch (result.StatusCode)
        {
          case HttpStatusCode.NotFound:
            toastService.ShowInfo($"Errore Stampa Scontrino di Verifica");

            break;
          default:
            break;
        }
      }
      catch (Exception ex)
      {
        toastService.ShowError(ex.Message);
        Log.Error($"{clientInfo.IPAddress} - OnPrintRequest_StampaDiProva - {ex.Message}");
      }
    }


    [JSInvokable("OnPrintRequest")]   // Ristampa Ultimo Ordine
    public async Task OnPrintRequest(bool _PrintEnabled, int idOrdine)
    {
      try
      {
        if (_PrintEnabled)
        {
          ArchOrdini _ordine = (await festeDataAccess.GetGenericQuery<ArchOrdini>($"SELECT * FROM arch_ordini WHERE IdOrdine = {idOrdine}")).ToList().FirstOrDefault();
          List<ArchOrdiniRighe> _ordineRighe = (await festeDataAccess.GetGenericQuery<ArchOrdiniRighe>($"SELECT r.* FROM arch_ordini o JOIN arch_ordini_righe r ON o.IdOrdine = r.IdOrdine WHERE o.IdOrdine = {idOrdine}")).ToList();

          //HttpResponseMessage result = (await RichiediStampaScontrino(idOrdine));
          HttpResponseMessage result = (await RichiediStampaScontrino(_ordine, _ordineRighe));

          switch (result.StatusCode)
          {
            case HttpStatusCode.NotFound:
              toastService.ShowInfo($"Errore Ristampa Ordine # {idOrdine}");

              break;
            default:
              break;
          }
        }
      }
      catch (Exception ex)
      {
        toastService.ShowError(ex.Message);
        Log.Error($"{clientInfo.IPAddress} - OnPrintRequest - {ex.Message}");

        //_ = ex;
        //Console.WriteLine("I/O error");
        //Log.Fatal(ex, "I/O error:");
      }
    }

    [JSInvokable("OnPrintRequest_Consumi")]
    public async Task OnPrintRequest_Consumi(bool flagCumulativo)
    {
      try
      {
        HttpResponseMessage result = (await RichiediStampaConsumi(Cassa, flagCumulativo));

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
        Log.Error($"{clientInfo.IPAddress} - OnPrintRequest_Consumi - {ex.Message}");

        //_ = ex;
        //Console.WriteLine("I/O error");
        //Log.Fatal(ex, "I/O error:");
      }
    }
    #endregion

    #region Metodi
    private void AggiornaDatiCassa()
    {
      Cassa = _UserInterfaceService.AnagrCasse.Where(w => w.IdCassa == IdCassa).FirstOrDefault();

      if (Cassa != null)
      {
        strTitolo = $"Gestione {Cassa.Cassa}";

#if THREADSAFE
        IEnumerable<ArchOrdini> ordiniDellaCassa = _UserInterfaceService.QryOrdini.Where(w => (w.Value.IdCassa == IdCassa)).Select(s => s.Value);
#else
        IEnumerable<ArchOrdini> ordiniDellaCassa = _UserInterfaceService.QryOrdini.Where(w => (w.IdCassa == IdCassa));
#endif
        Cassa.OrdiniDellaCassa = ordiniDellaCassa.Count();
        if (Cassa.OrdiniDellaCassa > 0)
        {
          Cassa.idUltimoOrdine = ordiniDellaCassa.OrderByDescending(o => o.IdOrdine).FirstOrDefault().IdOrdine;
        }
#if THREADSAFE
        TabellaProdotti = _UserInterfaceService.AnagrProdotti.Values.Where(w => Cassa.prodottiVisibili.Contains(w.IdProdotto)).ToList();
#else
        TabellaProdotti = _UserInterfaceService.AnagrProdotti.Where(w => Cassa.prodottiVisibili.Contains(w.IdProdotto)).ToList();
#endif
      }
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
        Log.Error($"{clientInfo.IPAddress} - HttpRequestToPrinterController 1 - {ex.Message}");
      }
      catch (TaskCanceledException ex)
      {
        if (ex.CancellationToken == cts.Token)
        {
          // a real cancellation, triggered by the caller
          toastService.ShowError($"{ex.Message}"); // "Errore Stampante"
          Log.Error($"{clientInfo.IPAddress} - HttpRequestToPrinterController 2 - {ex.Message}");
        }
        else
        {
          // a web request timeout (possibly other things!?)
          toastService.ShowError($"Problemi Accesso PrinterServer - {_ClientIPAddress}"); // "Errore Stampante"
          Log.Error($"{clientInfo.IPAddress} - HttpRequestToPrinterController 3 - {ex.Message}");
        }
      }
      return (result);
    }

    private async Task RescanSerialPorts(AnagrCasse _cassa, bool LogEnabled)
    {
      try
      {
        string ClientIPAddress;
        if (_cassa.IsRemote.Value == true)
        {
          // Se la stampante è Remota recupero l'indirizzo nel campo RemoteAddress
          ClientIPAddress = _cassa.RemoteAddress;
        }
        else
        {
          // Se la stampante è Locale significa che è attaccata al PC Client da cui stò compilando l'ordine quindi la devo cercare sull'indirizzo del client
          ClientIPAddress = clientInfo.IPAddress;
        }
        await HttpRequestToPrinterController<bool>(ClientIPAddress, "rescanSerialPorts", LogEnabled);
      }
      catch (Exception ex)
      {
        toastService.ShowError(ex.Message);
        Log.Error($"{clientInfo.IPAddress} - RescanSerialPorts - {ex.Message}");
      }
    }
    private async Task<HttpResponseMessage> RichiediStampaDiProva(AnagrCasse _cassa)
    {
      HttpResponseMessage result = new HttpResponseMessage();

      try
      {
        // Locale o Remota deve essere interpretato dal punto di vista della Cassa (cioè del Client)
        string ClientIPAddress = string.Empty;
        if (_cassa.IsRemote.Value == true)
        {
          // Se la stampante è Remota recupero l'indirizzo nel campo RemoteAddress
          ClientIPAddress = _cassa.RemoteAddress;
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
          Stampante = _cassa.PortName,
          DebugText = $"{_cassa.Cassa} - Stampa di Prova",
          LogEnabled = true,
          rawData = scontrino.Prepara_StampaDiProva(
            $"{_UserInterfaceService.DtFestaInCorso.ToString("ddd dd/MM").ToUpper()} - {(_UserInterfaceService.DtFestaInCorso.Hour == 12 ? "PRANZO" : "CENA")}",
            _UserInterfaceService.ArchFesta,
            _cassa)
        };
        await HttpRequestToPrinterController<StampaOrdine_RawData>(ClientIPAddress, "rawData", stampaOrdineRawData);
      }
      catch (Exception ex)
      {
        toastService.ShowError(ex.Message);
        Log.Error($"{clientInfo.IPAddress} - RichiediStampaDiProva - {ex.Message}");
      }
      return (result);
    }
    private async Task<HttpResponseMessage> RichiediStampaScontrino(ArchOrdini _ordine, List<ArchOrdiniRighe> _archOrdiniRighe)
    {
      HttpResponseMessage result = new HttpResponseMessage();

      try
      {
        AnagrCasse _cassa = _UserInterfaceService.AnagrCasse.Where(w => w.IdCassa == _ordine.IdCassa).FirstOrDefault();

        List<AnagrListe> _listeDaStampare = new List<AnagrListe>();
        List<ArchOrdiniRighe> _queueTicketDaStampare = new List<ArchOrdiniRighe>();

        // Recupero l'elenco delle categorie presenti nelle righe dell'ordine
        List<int> CategorieDaOrdine = _archOrdiniRighe.Select(s => s.IdCategoria).Distinct().ToList();

        if (_ordine.TipoOrdine == "BANCO")
        {
          _listeDaStampare = _UserInterfaceService.AnagrListe.Where(w => ((w.Banco_StampaScontrino == true) && (CategorieDaOrdine.Contains(w.IdLista)))).ToList();

#if THREADSAFE
          _queueTicketDaStampare = (from p in _UserInterfaceService.AnagrProdotti.Values.Where(w => w.PrintQueueTicket)
#else
          _queueTicketDaStampare = (from p in _UserInterfaceService.AnagrProdotti.Where(w => w.PrintQueueTicket)
#endif
                                    join r in _archOrdiniRighe on p.IdProdotto equals r.IdProdotto
                                    select r).ToList();
        }
        else
        {
          _listeDaStampare = _UserInterfaceService.AnagrListe.Where(w => ((w.Tavolo_StampaScontrino == true) && (CategorieDaOrdine.Contains(w.IdLista)))).ToList();
        }

        StampaOrdine_Scontrino stampaOrdine = new StampaOrdine_Scontrino
        {
          strNomeGiornata = _ordine.strDataAssegnazione, //  _UserInterfaceService.DtFestaInCorso.ToString("ddd dd/MM/yyyy HH:mm"),
          Cassa = _cassa,
          Festa = _UserInterfaceService.ArchFesta,
          Ordine = _ordine,
          RigheOrdine = _archOrdiniRighe,
          ListeDaStampare = _listeDaStampare,
          QueueTicketDaStampare = _queueTicketDaStampare,
          ScontrinoMuto = _cassa.ScontrinoMuto.Value
        };

        // Locale o Remota deve essere interpretato dal punto di vista della Cassa (cioè del Client)
        string ClientIPAddress = string.Empty;

        if (_cassa.IsRemote.Value == true)
        {
          // Se la stampante è Remota recupero l'indirizzo nel campo RemoteAddress
          ClientIPAddress = _cassa.RemoteAddress;
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
          Stampante = _cassa.PortName,
          DebugText = $"{_cassa.Cassa} - Ordine #{stampaOrdine.Ordine.IdOrdine}",
          LogEnabled = true,
          rawData = scontrino.Prepara_Ordine(stampaOrdine)
        };
        await HttpRequestToPrinterController<StampaOrdine_RawData>(ClientIPAddress, "rawData", stampaOrdineRawData);
      }
      catch (Exception ex)
      {
        toastService.ShowError(ex.Message);
        Log.Error($"{clientInfo.IPAddress} - RichiediStampaScontrino - {ex.Message}");
      }
      return (result);
    }
    private async Task<HttpResponseMessage> RichiediStampaScontrinoCucina(ArchOrdini _ordine, List<ArchOrdiniRighe> _archOrdiniRighe)
    {
      HttpResponseMessage result = new HttpResponseMessage();

      try
      {
        AnagrCasse _cassa = _UserInterfaceService.AnagrCasse.Where(w => w.IdCassa == _ordine.IdCassa).FirstOrDefault();

        List<AnagrListe> _listeDaStampareInCucina = _UserInterfaceService.AnagrListe.Where(w => w.Cucina_StampaScontrino == true).ToList();

        var scontrino = new GestioneScontrini_Ordini();
        foreach (var item in _listeDaStampareInCucina)
        {
          //toastService.ShowError($"{item.Lista} - {item.IdStampante}");

          if (_archOrdiniRighe.Where(w => w.IdCategoria == item.IdLista).Any())
          {
            AnagrStampanti _stampante = _UserInterfaceService.AnagrStampanti.Where(w => w.IdStampante == item.IdStampante).FirstOrDefault();

            //Log.Information($"{_stampante.RemoteAddress} - {_stampante.SerialPort}");
            //toastService.ShowError($"{_stampante.RemoteAddress} - {_stampante.SerialPort}");

            StampaOrdine_ScontrinoCucina _scontrinoCucina = new()
            {
              strNomeGiornata = _ordine.strDataAssegnazione,
              Cassa = _cassa,
              Festa = _UserInterfaceService.ArchFesta,
              Ordine = _ordine,
              RigheOrdine = _archOrdiniRighe.Where(w => w.IdCategoria == item.IdLista).ToList(), //   _archOrdiniRighe,
              ListaDaStampare = item
            };
            StampaOrdine_RawData _stampaOrdineRawData = new()
            {
              IPAddress = _stampante.RemoteAddress,
              Stampante = _stampante.SerialPort,
              DebugText = $"ScontrinoCucina - {_stampante.RemoteAddress} - Ordine #{_ordine.IdOrdine}",
              LogEnabled = true,
              rawData = scontrino.Prepara_Ordine_Cucina(_scontrinoCucina)
            };
            await HttpRequestToPrinterController<StampaOrdine_RawData>(_stampante.RemoteAddress, "rawData", _stampaOrdineRawData);
          }
        }
      }
      catch (Exception ex)
      {
        toastService.ShowError(ex.Message);
        Log.Error($"{clientInfo.IPAddress} - RichiediStampaScontrinoCucina - {ex.Message}");
      }
      return (result);
    }
    private async Task<HttpResponseMessage> RichiediStampaConsumi(AnagrCasse _cassa, bool _flagCumulativo)
    {
      HttpResponseMessage result = new HttpResponseMessage();
      StampaOrdine_ConsumiGiornata consumiGiornata;

      try
      {
        // Recupero i dati dell'ordine dal database
#if THREADSAFE
        var DatiFestaInCorso = from o in _UserInterfaceService.QryOrdini.Where(w => w.Value.DataAssegnazione == _UserInterfaceService.DtFestaInCorso)
                               join r in _UserInterfaceService.QryOrdiniRighe on o.Key equals r.Key.Item1
                               select new { o, r };
#else
        var DatiFestaInCorso = from o in _UserInterfaceService.QryOrdini.Where(w => w.DataAssegnazione == _UserInterfaceService.DtFestaInCorso)
                               join r in _UserInterfaceService.QryOrdiniRighe on o.IdOrdine equals r.IdOrdine
                               select new { o, r };
#endif
        if (DatiFestaInCorso.Any())
        {
          if (_flagCumulativo)
          {
#if THREADSAFE
            var _qryStatoCasse = (from r in DatiFestaInCorso
                                    //where r.o.IdCassa == _cassa.IdCassa
                                  group r by new { r.o.Value.IdCassa, r.r.Value.IdProdotto } into g
                                  orderby g.Key.IdCassa
                                  select new
                                  {
                                    IdCassa = g.Key.IdCassa,
                                    IdProdotto = g.Key.IdProdotto,
                                    Importo = g.Sum(s => s.r.Value.Importo),
                                    QuantitàProdotto = g.Sum(s => s.r.Value.QuantitàProdotto),
                                  });
            // Recupero l'elenco dei prodotti usati nella festa selezionata
            var _qryStatoProdotti = (from r in DatiFestaInCorso
                                       //where r.o.IdCassa == _cassa.IdCassa
                                     group r by r.r.Value.IdProdotto into g
                                     orderby g.Key
                                     select new
                                     {
                                       IdProdotto = g.Key,
                                       Importo = g.Sum(s => s.r.Value.Importo),
                                       QuantitàProdotto = g.Sum(s => s.r.Value.QuantitàProdotto),
                                     });
#else
            var _qryStatoCasse = (from r in DatiFestaInCorso
                                    //where r.o.IdCassa == _cassa.IdCassa
                                  group r by new { r.o.IdCassa, r.r.IdProdotto } into g
                                  orderby g.Key.IdCassa
                                  select new
                                  {
                                    IdCassa = g.Key.IdCassa,
                                    IdProdotto = g.Key.IdProdotto,
                                    Importo = g.Sum(s => s.r.Importo),
                                    QuantitàProdotto = g.Sum(s => s.r.QuantitàProdotto),
                                  });
            // Recupero l'elenco dei prodotti usati nella festa selezionata
            var _qryStatoProdotti = (from r in DatiFestaInCorso
                                       //where r.o.IdCassa == _cassa.IdCassa
                                     group r by r.r.IdProdotto into g
                                     orderby g.Key
                                     select new
                                     {
                                       IdProdotto = g.Key,
                                       Importo = g.Sum(s => s.r.Importo),
                                       QuantitàProdotto = g.Sum(s => s.r.QuantitàProdotto),
                                     });
#endif
            consumiGiornata = new StampaOrdine_ConsumiGiornata
            {
              IPAddress = clientInfo.IPAddress,
              //strNomeGiornata = _UserInterfaceService.DtFestaInCorso.ToString("ddd dd/MM/yyyy HH:mm"),
              strNomeGiornata = $"{_UserInterfaceService.DtFestaInCorso.ToString("ddd dd/MM").ToUpper()} - {(_UserInterfaceService.DtFestaInCorso.Hour == 12 ? "PRANZO" : "CENA")}",
              flagCumulativo = _flagCumulativo,
              Cassa = _cassa,
              Festa = _UserInterfaceService.ArchFesta,
#if THREADSAFE
              statoCassa = (from p in _UserInterfaceService.AnagrProdotti.Values
#else
              statoCassa = (from p in _UserInterfaceService.AnagrProdotti
#endif
                            join r in _qryStatoProdotti on p.IdProdotto equals r.IdProdotto
                            orderby p.IdProdotto
                            //where p.Stato
                            select new StampaOrdine_StatoCassa
                            {
                              IdProdotto = p.IdProdotto,
                              NomeProdotto = p.NomeProdotto.CR_to_Space(),
                              Importo = r.Importo,
                              Quantità = r.QuantitàProdotto,
                              statoCassa = (from c in _qryStatoCasse
                                              //where c.IdCassa == _cassa.IdCassa
                                            group c by c.IdCassa into g
                                            orderby g.Key
                                            select new StatoCasse
                                            {
                                              IdCassa = g.Key,
                                              Importo = g.Where(c => c.IdProdotto == p.IdProdotto).Sum(s => s.Importo),
                                              QuantitàProdotto = g.Where(c => c.IdProdotto == p.IdProdotto).Sum(s => s.QuantitàProdotto)
                                            }
                                            ).FirstOrDefault(),
                            }).ToList()
            };
          }
          else
          {
#if THREADSAFE
            var _qryStatoCasse = (from r in DatiFestaInCorso
                                  where r.o.Value.IdCassa == _cassa.IdCassa
                                  group r by new { r.o.Value.IdCassa, r.r.Value.IdProdotto } into g
                                  orderby g.Key.IdCassa
                                  select new
                                  {
                                    IdCassa = g.Key.IdCassa,
                                    IdProdotto = g.Key.IdProdotto,
                                    Importo = g.Sum(s => s.r.Value.Importo),
                                    QuantitàProdotto = g.Sum(s => s.r.Value.QuantitàProdotto),
                                  });

            var _qryStatoProdotti = (from r in DatiFestaInCorso
                                     where r.o.Value.IdCassa == _cassa.IdCassa
                                     group r by r.r.Value.IdProdotto into g
                                     orderby g.Key
                                     select new
                                     {
                                       IdProdotto = g.Key,
                                       Importo = g.Sum(s => s.r.Value.Importo),
                                       QuantitàProdotto = g.Sum(s => s.r.Value.QuantitàProdotto),
                                     });
#else
            var _qryStatoCasse = (from r in DatiFestaInCorso
                                  where r.o.IdCassa == _cassa.IdCassa
                                  group r by new { r.o.IdCassa, r.r.IdProdotto } into g
                                  orderby g.Key.IdCassa
                                  select new
                                  {
                                    IdCassa = g.Key.IdCassa,
                                    IdProdotto = g.Key.IdProdotto,
                                    Importo = g.Sum(s => s.r.Importo),
                                    QuantitàProdotto = g.Sum(s => s.r.QuantitàProdotto),
                                  });

            var _qryStatoProdotti = (from r in DatiFestaInCorso
                                     where r.o.IdCassa == _cassa.IdCassa
                                     group r by r.r.IdProdotto into g
                                     orderby g.Key
                                     select new
                                     {
                                       IdProdotto = g.Key,
                                       Importo = g.Sum(s => s.r.Importo),
                                       QuantitàProdotto = g.Sum(s => s.r.QuantitàProdotto),
                                     });
#endif
            consumiGiornata = new StampaOrdine_ConsumiGiornata
            {
              IPAddress = clientInfo.IPAddress,
              //strNomeGiornata = _UserInterfaceService.DtFestaInCorso.ToString("ddd dd/MM/yyyy HH:mm"),
              strNomeGiornata = $"{_UserInterfaceService.DtFestaInCorso.ToString("ddd dd/MM").ToUpper()} - {(_UserInterfaceService.DtFestaInCorso.Hour == 12 ? "PRANZO" : "CENA")}",
              flagCumulativo = _flagCumulativo,
              Cassa = _cassa,
              Festa = _UserInterfaceService.ArchFesta,
#if THREADSAFE
              statoCassa = (from p in _UserInterfaceService.AnagrProdotti.Values
#else
              statoCassa = (from p in _UserInterfaceService.AnagrProdotti
#endif
                            join r in _qryStatoProdotti on p.IdProdotto equals r.IdProdotto
                            orderby p.IdProdotto
                            //where p.Stato
                            select new StampaOrdine_StatoCassa
                            {
                              IdProdotto = p.IdProdotto,
                              NomeProdotto = p.NomeProdotto.CR_to_Space(),
                              Importo = r.Importo,
                              Quantità = r.QuantitàProdotto,
                              statoCassa = (from c in _qryStatoCasse
                                            where c.IdCassa == _cassa.IdCassa
                                            group c by c.IdCassa into g
                                            orderby g.Key
                                            select new StatoCasse
                                            {
                                              IdCassa = g.Key,
                                              Importo = g.Where(c => c.IdProdotto == p.IdProdotto).Sum(s => s.Importo),
                                              QuantitàProdotto = g.Where(c => c.IdProdotto == p.IdProdotto).Sum(s => s.QuantitàProdotto)
                                            }
                                            ).FirstOrDefault(),
                            }).ToList()
            };
          }

          // Locale o Remota deve essere interpretato dal punto di vista della Cassa (cioè del Client)
          string ClientIPAddress = string.Empty;
          if (_cassa.IsRemote.Value == true)
          {
            // Se la stampante è Remota recupero l'indirizzo nel campo RemoteAddress
            ClientIPAddress = _cassa.RemoteAddress;
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
            Stampante = _cassa.PortName,
            DebugText = $"{_cassa.Cassa} - Stampa Consumi",
            LogEnabled = true,
            rawData = scontrino.Prepara_Consumi(consumiGiornata)
          };
          await HttpRequestToPrinterController<StampaOrdine_RawData>(ClientIPAddress, "rawData", stampaOrdineRawData);
        }
      }
      catch (Exception ex)
      {
        toastService.ShowError(ex.Message);
        Log.Error($"{clientInfo.IPAddress} - RichiediStampaConsumi - {ex.Message}");
      }
      return (result);
    }
    private async void OnDataOraServerChanged(object sender, DateTime adesso)
    {
      try
      {
        if (Module is not null)
        {
          await Module.InvokeVoidAsync("GestioneCassaObj.updateDataOra", adesso.ToString("HH:mm:ss"));
        }
      }
      catch
      {
        // Log.Error($"{clientInfo.IPAddress} - OnDataOraServerChanged - {ex.Message}");
      }
    }
    private async void OnNotifyStatoProdotti(object sender, int idCassa)
    {
      try
      {
        AggiornaDatiCassa();

        if (Module is not null)
        {
          await Module.InvokeVoidAsync("GestioneCassaObj.updateStatoProdotti", TabellaProdotti);
        }
      }
      catch (Exception ex)
      {
        Log.Error($"{clientInfo.IPAddress} - OnNotifyStatoProdotti - {ex.Message}");
      }
    }

    private async void OnNotifyAnagrProdotti(object sender, bool _refresh)
    {
      if (Cassa is null)
      {
        return;
      }

      try
      {
#if THREADSAFE
        TabellaProdotti = _UserInterfaceService.AnagrProdotti.Values.Where(w => Cassa.prodottiVisibili.Contains(w.IdProdotto)).ToList();
#else
        TabellaProdotti = _UserInterfaceService.AnagrProdotti.Where(w => Cassa.prodottiVisibili.Contains(w.IdProdotto)).ToList();
#endif
        if (Module is not null)
        {
          await Module.InvokeVoidAsync("GestioneCassaObj.updateAnagrProdotti", IdCassa, TabellaProdotti);
        }
      }
      catch (Exception ex)
      {
        Log.Error($"{clientInfo.IPAddress} - OnNotifyStatoProdotti - {ex.Message}");
      }
    }
    #endregion
  }
}




