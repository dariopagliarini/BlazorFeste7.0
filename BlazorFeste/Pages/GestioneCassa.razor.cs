using Blazored.Toast.Services;

using BlazorFeste.Constants;
using BlazorFeste.Data.Models;
using BlazorFeste.DataAccess;
using BlazorFeste.lib;
using BlazorFeste.Services;
using BlazorFeste.Services.AppOrdini;
using BlazorFeste.Util;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

using Newtonsoft.Json;

using Serilog;

using System.Net;
using System.Text.RegularExpressions;

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
    [Inject] public FesteDataAccess _FesteDataAccess { get; init; }
    [Inject] public IJSRuntime JSRuntime { get; init; }
    [Inject] public IAppOrdiniService appOrdiniState { get; init; }
    #endregion

    #region Variabili
    private IJSObjectReference Module;
    private Task<IJSObjectReference> _jsModule;
    private Task<IJSObjectReference> JsModule => _jsModule ??= JSRuntime.InvokeAsync<IJSObjectReference>("import", "./js/GestioneCassaObj.js").AsTask();

    private DotNetObjectReference<GestioneCassa> objRef;
    bool flagInizializza { get; set; } = false;

    AnagrCasse Cassa { get; set; } = new AnagrCasse();
    public string strTitolo { get; set; } = "Cassa Non Abilitata";
    List<AnagrProdotti> TabellaProdotti { get; set; }
    #endregion

    #region LifeCycle
    protected override Task OnInitializedAsync()
    {
      _UserInterfaceService.NotifyDataOraServer += OnNotifyDataOraServer;
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
          await Module.InvokeVoidAsync("GestioneCassaObj.init", objRef, Cassa, _UserInterfaceService.ArchFesta.WebAppAttiva);

          if (Cassa.ScontrinoAbilitato.Value)
            await RescanSerialPorts(Cassa, true);
        }
      }
      await base.OnAfterRenderAsync(firstRender);
    }
    protected override async Task OnParametersSetAsync()
    {
      Cassa = _UserInterfaceService.AnagrCasse.Where(w => w.IdCassa == IdCassa).FirstOrDefault();

      if (Cassa != null)
      {
        strTitolo = $"Gestione {Cassa.Cassa}";

        AggiornaDatiCassa(_UserInterfaceService.AnagrProdotti.Values.ToList());

        flagInizializza = true;
      }
      await base.OnParametersSetAsync();
    }

    public void Dispose()
    {
      _UserInterfaceService.NotifyDataOraServer -= OnNotifyDataOraServer;
      _UserInterfaceService.NotifyStatoProdotti -= OnNotifyStatoProdotti;
      _UserInterfaceService.NotifyAnagrProdotti -= OnNotifyAnagrProdotti;

      _jsModule?.Result.InvokeVoidAsync("GestioneCassaObj.dispose");

      objRef?.Dispose();
    }
    #endregion

    #region JSInvokable
    [JSInvokable("OnSaveToMySQLAsync")]
    public async Task<string> OnSaveToMySQLAsync(
        bool _PrintEnabled
      , string _TipoOrdine
      , string _Tavolo
      , string _Coperti
      , string _NotaOrdine
      , string _Referente
      , bool _PagamentoConPOS
      , int _APPIdOrdine
      , List<RigaCassa> Righe)
    {
      // Inizializzazione Record ArchOrdini
      ArchOrdini _archOrdine = new ArchOrdini
      {
        Cassa = IdCassa.ToString(),
        TipoOrdine = _TipoOrdine,
        Tavolo = _Tavolo,
        NumeroCoperti = _Coperti,
        NoteOrdine = _NotaOrdine,
        Referente = _Referente,
        PagamentoConPOS = _PagamentoConPOS,
        DataOra = DateTime.Now,
        AppIdOrdine = _APPIdOrdine,

        // Prima venivano gestiti nel trigger
        IdFesta = _UserInterfaceService.ArchFesta.IdFesta,
        DataAssegnazione = _UserInterfaceService.DtFestaInCorso,
        IdStatoOrdine = (int)K_STATO_ORDINE.AppenaNato, // 0

        // Gestito nel trigger MySQL "arch_ordini_before_insert"
        //ProgressivoSerata = _UserInterfaceService.QryOrdini.Count() + 1,
      };

      // Inizializzazione Record ArchOrdiniRighe
      List<ArchOrdiniRighe> _archOrdineRighe = new List<ArchOrdiniRighe>();

      // Verifico se l'ordine ha qualche tipologia di sconto
      double _TotaleOrdine = Righe.Sum(s => (s.PrezzoUnitario * s.QuantitàProdotto));
      if (_TotaleOrdine < 0)
      {
        // Devo risistemare il valore del buono
        int _NumeroBuoni = Righe.Where(w => w.PrezzoUnitario < 0).Sum(s => s.QuantitàProdotto);

        foreach (var item in Righe.Where(w => w.PrezzoUnitario < 0)) // Ricalcolo il valore del buono nel caso non venga speso completamente
        {
          item.PrezzoUnitario = item.PrezzoUnitario - (_TotaleOrdine / _NumeroBuoni);
        }
      }

      int iRiga = 1;
      foreach (var item in Righe.Where(w => w.QuantitàProdotto > 0))
      {
        var a = (from prod in _UserInterfaceService.AnagrProdotti.Values.Where(w => w.IdProdotto == item.IdProdotto)
                 join lista in _UserInterfaceService.AnagrListe on prod.IdLista equals lista.IdLista
                 select new { prod, lista }).FirstOrDefault();

        _archOrdineRighe.Add(new ArchOrdiniRighe
        {
          IdOrdine = 0,
          IdRiga = iRiga++,
          IdCategoria = a.prod.IdLista,
          Categoria = a.lista.Lista,
          IdProdotto = item.IdProdotto,
          NomeProdotto = item.NomeProdotto,
          IdStatoRiga = (int)K_STATO_RIGA.OrdineAppenaNato, //   0,
          QuantitàProdotto = item.QuantitàProdotto,
          QuantitàEvasa = 0,
          Importo = item.PrezzoUnitario * item.QuantitàProdotto,
          DataOra_RigaPresaInCarico = default(DateTime),
          DataOra_RigaEvasa = default(DateTime),
          QueueTicket = a.prod.Consumo + 1
        });
      }

      if (Cassa.SoloBanco.Value)
      {
        // Se l'ordine arriva da una cassa "SoloBanco" devo evadere l'ordine
        _archOrdine.IdStatoOrdine = (int)K_STATO_ORDINE.InCorso; //  2;

        // Tutte le righe dell'ordine vengono messe nello stato 3
        foreach (var item in _archOrdineRighe)
        {
          item.IdStatoRiga = (int)K_STATO_RIGA.OrdineEvaso; // 3;
          item.QuantitàEvasa = item.QuantitàProdotto;
          item.DataOra_RigaPresaInCarico = DateTime.Now;
          item.DataOra_RigaEvasa = DateTime.Now;
        }
      }
      else
      {
        #region Evasione Liste non gestite (Abilitate ma non visibili)
        foreach (var lista in _UserInterfaceService.AnagrListe.Where(w => w.Visibile == false))
        {
          if (_archOrdineRighe.Select(s => s.IdCategoria).Contains(lista.IdLista))
          {
            // Tutte le righe dell'ordine che appartengono alla lista vengono messe nello stato 3 (Ordine Evaso x questa lista)
            var Prodotti = (_UserInterfaceService.AnagrProdotti.Values.Where(w => w.IdLista == lista.IdLista)).ToList();
            var Prodotto = Prodotti.Select(s => s.IdProdotto).ToArray();

            foreach (var riga in _archOrdineRighe.Where(w => Prodotto.Contains(w.IdProdotto)))
            {
              riga.IdStatoRiga = (int)K_STATO_RIGA.OrdineEvaso; // 3; 
              riga.QuantitàEvasa = riga.QuantitàProdotto;
              riga.DataOra_RigaPresaInCarico = DateTime.Now;
              riga.DataOra_RigaEvasa = DateTime.Now;
            }

            // Se la lista è una lista padre (IoSonoListaPadre = true)
            if (lista.IoSonoListaPadre == true)
            {
              var OrdinePre = _archOrdine;

              // Sblocco le liste figlio
              _archOrdine.IdStatoOrdine = (int)K_STATO_ORDINE.InCorso; //  2;

              // Tutte le righe dell'ordine che fanno parte delle liste figlio devo essere riconoscibili - Metto il loro IdStatoRiga a -1
              var ListeFiglio = _UserInterfaceService.AnagrListe.Where(w => w.IdListaPadre == lista.IdLista).Select(s => s.IdLista).ToArray();
              var RigheDelleListeFiglio = (from r in _archOrdineRighe
                                           join p in Prodotti.Where(w => ListeFiglio.Contains(w.IdLista)) on r.IdProdotto equals p.IdProdotto
                                           select r.IdRiga).ToArray();

              foreach (var item in _archOrdineRighe.Where(w => RigheDelleListeFiglio.Contains(w.IdRiga)))
              {
                item.IdStatoRiga = -1;
              }
            }
          }
        }
        #endregion

        #region Gestione Liste da prendere in carico automaticamente (Abilitate, Visibili e con StampaCucina)
        foreach (var lista in _UserInterfaceService.AnagrListe.Where(w => (w.Visibile == true) && (w.Cucina_StampaScontrino == true)))
        {
          if (_archOrdineRighe.Select(s => s.IdCategoria).Contains(lista.IdLista))
          {
            // Tutte le righe dell'ordine che appartengono alla lista vengono messe nello stato 1 (Presa In Carico)
            var Prodotti = (_UserInterfaceService.AnagrProdotti.Values.Where(w => w.IdLista == lista.IdLista)).ToList();
            var Prodotto = Prodotti.Select(s => s.IdProdotto).ToArray();

            foreach (var riga in _archOrdineRighe.Where(w => Prodotto.Contains(w.IdProdotto)))
            {
              riga.IdStatoRiga = (int)K_STATO_RIGA.PresaInCarico;
              riga.DataOra_RigaPresaInCarico = DateTime.Now;
            }
          }
        }
        #endregion

        // Verifico se esistono righe dell'ordine con IdListaPadre > 0 (cioè con delle code a priorità maggiore ancora da smaltire)
        _archOrdine.IdStatoOrdine = GetStatoOrdine(_archOrdine, _archOrdineRighe);

        _archOrdine.Tavolo = _archOrdine.TipoOrdine.CompareTo("SERVITO") == 0 ? string.Format("{0} {1}", _archOrdine.Tavolo, Regex.Replace(_archOrdine.NumeroCoperti, @"[^a-zA-Z]+", string.Empty).ToUpper()).Trim() : _archOrdine.TipoOrdine;
        _archOrdine.NumeroCoperti = Regex.Replace(_archOrdine.NumeroCoperti, @"[^0-9]+", string.Empty).Trim();
      }

      // Creo l'ordine nel database e assegno alle variabili di memoria l'idOrdine che mi arriva dal DB prima di creare l'ordine nelle liste globali
      _archOrdine.IdOrdine = await _FesteDataAccess.InsertArchOrdiniAsync(_archOrdine, _archOrdineRighe);

      // Creo l'ordine nella lista in memoria
      if (_UserInterfaceService.QryOrdini.TryAdd(_archOrdine.IdOrdine, _archOrdine))
      {
        foreach (var _archOrdineRiga in _archOrdineRighe)
        {
          _archOrdineRiga.IdOrdine = _archOrdine.IdOrdine;
          if (!_UserInterfaceService.QryOrdiniRighe.TryAdd(new Tuple<long, int>(_archOrdine.IdOrdine, _archOrdineRiga.IdRiga), _archOrdineRiga))
          {
            Log.Warning($"DatabaseTimerService - GetDatabaseData - Insert OrdineRiga - {_archOrdine.IdOrdine}/{_archOrdineRiga.IdRiga} non inserita");
          }
        }
      }
      else
      {
        Log.Warning($"DatabaseTimerService - GetDatabaseData - Insert Ordine - {_archOrdine.IdOrdine} non inserito");
      }

      // Se arrivo qui significa che :
      //    Devo notificare a chi lo desidera i dati del nuovo ordine
      //      _UserInterfaceService.OnNotifyStatoOrdine(idOrdine);
      _UserInterfaceService.OnNotifyNuovoOrdine(new DatiOrdine { ordine = _archOrdine, ordineRighe = _archOrdineRighe });

      toastService.ShowSuccess($"Ordine #{_archOrdine.IdOrdine} creato con successo"); // , "Avanti il prossimo"

      // Evado l'eventuale ordine su Cloud
      if (_archOrdine.AppIdOrdine > 0)
      {
        appOrdiniState.AddOrdineDaEvadere((int)_archOrdine.AppIdOrdine);
      }

      #region Gestione Stampa 
      try
      {
        // Verifico se ho qualche riga da stampare anche in cucina (solo se Ordine== "SERVITO")
        var _RigheDaStampareInCucina = (from l in _UserInterfaceService.AnagrListe.Where(w => w.Cucina_StampaScontrino == true)
                                        join r in _archOrdineRighe on l.IdLista equals r.IdCategoria
                                        select r.IdRiga).Any()
                                        &&
                                        (_TipoOrdine == "SERVITO");

        if (_PrintEnabled || _RigheDaStampareInCucina)
        {
          if (_PrintEnabled)
          {
            HttpResponseMessage result = (await RichiediStampaScontrino(_archOrdine, _archOrdineRighe));
            switch (result.StatusCode)
            {
              case HttpStatusCode.NotFound:
                toastService.ShowInfo($"Errore Stampa Ordine {_archOrdine.DataOra:dd/MM/yyyy HH:mm:ss)}");

                break;
              default:
                break;
            }
          }

          if (_RigheDaStampareInCucina)
          {
            HttpResponseMessage result = (await RichiediStampaScontrinoCucina(_archOrdine, _archOrdineRighe));

            switch (result.StatusCode)
            {
              case HttpStatusCode.NotFound:
                toastService.ShowInfo($"Errore Stampa Ordine {_archOrdine.DataOra:dd/MM/yyyy HH:mm:ss}");

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
        Log.Error(ex, $"{clientInfo.IPAddress} - Code Exception");
        //_ = ex;
        //Console.WriteLine("I/O error");
        //Log.Fatal(ex, "I/O error:");
      }
      #endregion

      Log.Information($"{clientInfo.IPAddress} - Nuovo Ordine - {_archOrdine.IdOrdine}, Cassa: {_archOrdine.Cassa}, RigheOrdine: {_archOrdineRighe.Count}");

      double ImportoContanti = 0.0;
      double ImportoPOS = 0.0;

      var DatiCassa = await _FesteDataAccess.GetDatiCassaAsync(_archOrdine.IdCassa, _archOrdine.DataAssegnazione);
      if (DatiCassa.Any())
      {
        if (DatiCassa.Where(w => w.PagamentoConPOS == false).Count() > 0)
        {
          ImportoContanti = DatiCassa.Where(w => w.PagamentoConPOS == false).FirstOrDefault().Importo;
        }

        if (DatiCassa.Where(w => w.PagamentoConPOS == true).Count() > 0)
        {
          ImportoPOS = DatiCassa.Where(w => w.PagamentoConPOS == true).FirstOrDefault().Importo;
        }
      }

      Dictionary<string, object> sendDict = new Dictionary<string, object>();
      sendDict.Add("ultimoOrdine", _archOrdine.IdOrdine);
      sendDict.Add("cassaContanti", ImportoContanti);
      sendDict.Add("cassaPOS", ImportoPOS);

      return (JsonConvert.SerializeObject(sendDict));
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
        Log.Error(ex, $"{clientInfo.IPAddress} - Code Exception");
      }
    }

    [JSInvokable("OnPrintRequest")]   // Ristampa Ultimo Ordine
    public async Task OnPrintRequest(bool _PrintEnabled, int idOrdine)
    {
      try
      {
        if (_PrintEnabled)
        {
          ArchOrdini _ordine = _UserInterfaceService.QryOrdini.Values.Where(w => w.IdOrdine == idOrdine).FirstOrDefault();
          List<ArchOrdiniRighe> _ordineRighe = _UserInterfaceService.QryOrdiniRighe.Values.Where(w => w.IdOrdine == idOrdine).ToList();

          //ArchOrdini _ordine = (await festeDataAccess.GetGenericQuery<ArchOrdini>($"SELECT * FROM arch_ordini WHERE IdOrdine = {idOrdine}")).ToList().FirstOrDefault();
          //List<ArchOrdiniRighe> _ordineRighe = (await festeDataAccess.GetGenericQuery<ArchOrdiniRighe>($"SELECT r.* FROM arch_ordini o JOIN arch_ordini_righe r ON o.IdOrdine = r.IdOrdine WHERE o.IdOrdine = {idOrdine}")).ToList();

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
        Log.Error(ex, $"{clientInfo.IPAddress} - Code Exception");

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
        Log.Error(ex, $"{clientInfo.IPAddress} - Code Exception");
        //_ = ex;
        //Console.WriteLine("I/O error");
        //Log.Fatal(ex, "I/O error:");
      }
    }

    [JSInvokable("OnGetOrderFromCloud_Async")]
    public async Task<string> OnGetOrderFromCloud_Async(int idOrdine)
    {
      string jsonResponse = string.Empty;

      //if (idOrdine == 0)
      //{
      //  jsonResponse = appOrdiniState.GetListaOrdini();
      //}
      //else
      //{
      //  string ClientIPAddress = "https://ghqb.galileicrema.org/brolo/api";
      //  try
      //  {
      //    jsonResponse = await HttpRequestToCloud_Order<string>(ClientIPAddress, idOrdine);
      //  }
      //  catch (Exception ex)
      //  {
      //    toastService.ShowError(ex.Message);
      //    Log.Error(ex, $"{clientInfo.IPAddress} - Code Exception");
      //  }
      //}
      string ClientIPAddress = "https://ghqb.galileicrema.org/brolo/api";
      try
      {
        jsonResponse = await HttpRequestToCloud_Order<string>(ClientIPAddress, idOrdine);
      }
      catch (Exception ex)
      {
        toastService.ShowError(ex.Message);
        Log.Error(ex, $"{clientInfo.IPAddress} - Code Exception");
      }

      return (jsonResponse);
    }
    #endregion

    #region Metodi
    private void AggiornaDatiCassa(List<AnagrProdotti> statoProdotti)
    {
      //Cassa = _UserInterfaceService.AnagrCasse.Where(w => w.IdCassa == IdCassa).FirstOrDefault();

      if (Cassa != null)
      {
        IEnumerable<ArchOrdini> ordiniDellaCassa = _UserInterfaceService.QryOrdini.Where(w => (w.Value.IdCassa == IdCassa)).Select(s => s.Value);
        Cassa.OrdiniDellaCassa = ordiniDellaCassa.Count();
        if (Cassa.OrdiniDellaCassa > 0)
        {
          Cassa.idUltimoOrdine = ordiniDellaCassa.OrderByDescending(o => o.IdOrdine).FirstOrDefault().IdOrdine;
        }
        TabellaProdotti = statoProdotti.Where(w => Cassa.prodottiVisibili.Contains(w.IdProdotto)).ToList();
      }
    }
    private int GetStatoOrdine(ArchOrdini archOrdine, List<ArchOrdiniRighe> archOrdineRighe)
    {
      int Result = (int)K_STATO_ORDINE.InCorso;

      // Verifico se esistono righe dell'ordine con IdListaPadre > 0 (cioè con delle code a priorità maggiore ancora da smaltire)
      var RigheConListaPadre = from r in archOrdineRighe
                               join p in _UserInterfaceService.AnagrProdotti.Values on r.IdProdotto equals p.IdProdotto
                               join l in _UserInterfaceService.AnagrListe.Where(w => w.IdListaPadre > 0) on p.IdLista equals l.IdLista
                               select new { r, p, l };

      foreach (var Riga in RigheConListaPadre)
      {
        // Verifico se esistono ancora prodotti da smaltire nella lista padre
        var RigheListaPadre = from r in archOrdineRighe.Where(w => w.IdStatoRiga < (int)K_STATO_RIGA.OrdineEvaso)
                              join p in _UserInterfaceService.AnagrProdotti.Values on r.IdProdotto equals p.IdProdotto
                              join l in _UserInterfaceService.AnagrListe.Where(w => w.IdLista == Riga.l.IdListaPadre) on p.IdLista equals l.IdLista
                              select new { r, p, l };

        // Me ne basta una per decidere che lo stato dell'ordine é = 1 (Bloccato dalla lista Padre)
        if (RigheListaPadre.Any())
        {
          Result = (int)K_STATO_ORDINE.Bloccato; // Le liste con priorità = 2 devono aspettare
          break;
        }
      }
      return Result;
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
        Log.Error(ex, $"{clientInfo.IPAddress} - HttpRequestToPrinterController 1 - WebException");
      }
      catch (TaskCanceledException ex)
      {
        if (ex.CancellationToken == cts.Token)
        {
          // a real cancellation, triggered by the caller
          toastService.ShowError($"{ex.Message}"); // "Errore Stampante"
          Log.Error(ex, $"{clientInfo.IPAddress} - HttpRequestToPrinterController 2 - TaskCanceledException");
        }
        else
        {
          // a web request timeout (possibly other things!?)
          toastService.ShowError($"Problemi Accesso PrinterServer - {_ClientIPAddress}"); // "Errore Stampante"
          //Log.Error(ex, $"{clientInfo.IPAddress} - HttpRequestToPrinterController 3");
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
        Log.Error(ex, $"{clientInfo.IPAddress} - Code Exception");
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
        Log.Error(ex, $"{clientInfo.IPAddress} - Code Exception");
      }
      return (result);
    }
    private async Task<HttpResponseMessage> RichiediStampaScontrino(ArchOrdini _ordine, List<ArchOrdiniRighe> _archOrdiniRighe)
    {
      HttpResponseMessage result = new HttpResponseMessage();
      try
      {
        List<AnagrListe> _listeDaStampare = new List<AnagrListe>();
        List<ArchOrdiniRighe> _queueTicketDaStampare = new List<ArchOrdiniRighe>();

        // Recupero l'elenco delle categorie presenti nelle righe dell'ordine
        List<int> CategorieDaOrdine = _archOrdiniRighe.Select(s => s.IdCategoria).Distinct().ToList();

        if (_ordine.TipoOrdine == "BANCO")
        {
          _listeDaStampare = _UserInterfaceService.AnagrListe.Where(w => ((w.Banco_StampaScontrino == true) && (CategorieDaOrdine.Contains(w.IdLista)))).ToList();

          _queueTicketDaStampare = (from p in _UserInterfaceService.AnagrProdotti.Values.Where(w => w.PrintQueueTicket)
                                    join r in _archOrdiniRighe on p.IdProdotto equals r.IdProdotto
                                    select r).ToList();
        }
        else
        {
          _listeDaStampare = _UserInterfaceService.AnagrListe.Where(w => ((w.Tavolo_StampaScontrino == true) && (CategorieDaOrdine.Contains(w.IdLista)))).ToList();
        }

        //StampaOrdine_Scontrino stampaOrdine = new StampaOrdine_Scontrino
        //{
        //  strNomeGiornata = _ordine.strDataAssegnazione, 
        //  Cassa = _cassa,
        //  Festa = _UserInterfaceService.ArchFesta,
        //  Ordine = _ordine,
        //  RigheOrdine = _archOrdiniRighe,
        //  ListeDaStampare = _listeDaStampare,
        //  QueueTicketDaStampare = _queueTicketDaStampare,
        //  ScontrinoMuto = _cassa.ScontrinoMuto.Value
        //};

        StampaOrdine_Scontrino stampaOrdine = new StampaOrdine_Scontrino();
        try
        {
          stampaOrdine.strNomeGiornata = _ordine.strDataAssegnazione;
          stampaOrdine.Cassa = Cassa;
          stampaOrdine.Festa = _UserInterfaceService.ArchFesta;
          stampaOrdine.Ordine = _ordine;
          stampaOrdine.RigheOrdine = _archOrdiniRighe;
          stampaOrdine.ListeDaStampare = _listeDaStampare;
          stampaOrdine.QueueTicketDaStampare = _queueTicketDaStampare;
          stampaOrdine.ScontrinoMuto = Cassa.ScontrinoMuto.Value;
        }
        catch (Exception ex)
        {
          Log.Error(ex, $"{clientInfo.IPAddress} - stampaOrdine - Code Exception");
        }

        // Locale o Remota deve essere interpretato dal punto di vista della Cassa (cioè del Client)
        string ClientIPAddress = string.Empty;
        if (Cassa.IsRemote.Value == true)
        {
          // Se la stampante è Remota recupero l'indirizzo nel campo RemoteAddress
          ClientIPAddress = Cassa.RemoteAddress;
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
          Stampante = Cassa.PortName,
          DebugText = $"{Cassa.Cassa} - Ordine #{stampaOrdine.Ordine.IdOrdine}",
          LogEnabled = true,
          rawData = scontrino.Prepara_Ordine(stampaOrdine)
        };
        await HttpRequestToPrinterController<StampaOrdine_RawData>(ClientIPAddress, "rawData", stampaOrdineRawData);
      }
      catch (Exception ex)
      {
        toastService.ShowError(ex.Message);
        Log.Error(ex, $"{clientInfo.IPAddress} - Code Exception");
      }
      return (result);
    }
    private async Task<HttpResponseMessage> RichiediStampaScontrinoCucina(ArchOrdini _ordine, List<ArchOrdiniRighe> _archOrdiniRighe)
    {
      HttpResponseMessage result = new HttpResponseMessage();

      try
      {
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
              Cassa = Cassa,
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
        Log.Error(ex, $"{clientInfo.IPAddress} - Code Exception");
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
        var DatiFestaInCorso = from o in _UserInterfaceService.QryOrdini // .Where(w => w.Value.DataAssegnazione == _UserInterfaceService.DtFestaInCorso)
                               join r in _UserInterfaceService.QryOrdiniRighe on o.Key equals r.Key.Item1
                               select new { o, r };

        if (DatiFestaInCorso.Any())
        {
          if (_flagCumulativo)
          {
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
            consumiGiornata = new StampaOrdine_ConsumiGiornata
            {
              IPAddress = clientInfo.IPAddress,
              strNomeGiornata = $"{_UserInterfaceService.DtFestaInCorso.ToString("ddd dd/MM").ToUpper()} - {(_UserInterfaceService.DtFestaInCorso.Hour == 12 ? "PRANZO" : "CENA")}",
              flagCumulativo = _flagCumulativo,
              Cassa = _cassa,
              Festa = _UserInterfaceService.ArchFesta,
              statoCassa = (from p in _UserInterfaceService.AnagrProdotti.Values
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

            consumiGiornata = new StampaOrdine_ConsumiGiornata
            {
              IPAddress = clientInfo.IPAddress,
              //strNomeGiornata = _UserInterfaceService.DtFestaInCorso.ToString("ddd dd/MM/yyyy HH:mm"),
              strNomeGiornata = $"{_UserInterfaceService.DtFestaInCorso.ToString("ddd dd/MM").ToUpper()} - {(_UserInterfaceService.DtFestaInCorso.Hour == 12 ? "PRANZO" : "CENA")}",
              flagCumulativo = _flagCumulativo,
              Cassa = _cassa,
              Festa = _UserInterfaceService.ArchFesta,

              statoCassa = (from p in _UserInterfaceService.AnagrProdotti.Values
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
        Log.Error(ex, $"{clientInfo.IPAddress} - Code Exception");
      }
      return (result);
    }

    #region Ordini da WebApp 
    private async Task<string> HttpRequestToCloud_Order<T>(string _ClientIPAddress, int _idOrdine, int _TimeOutMSec = 5000)
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
        Log.Error(ex, $"{clientInfo.IPAddress} - HttpRequestToPrinterController 1 - WebException");
      }
      catch (TaskCanceledException ex)
      {
        if (ex.CancellationToken == cts.Token)
        {
          // a real cancellation, triggered by the caller
          toastService.ShowError($"{ex.Message}"); // "Errore Stampante"
          Log.Error(ex, $"{clientInfo.IPAddress} - HttpRequestToPrinterController 2 - TaskCanceledException");
        }
        else
        {
          // a web request timeout (possibly other things!?)
          toastService.ShowError($"Problemi Accesso PrinterServer - {_ClientIPAddress}"); // "Errore Stampante"
          //Log.Error(ex, $"{clientInfo.IPAddress} - HttpRequestToPrinterController 3");
        }
      }
      return (jsonResponse);
    }
    #endregion

    private async void OnNotifyDataOraServer(object sender, DateTime adesso)
    {
      try
      {
        if (Module is not null)
        {
          await Module.InvokeVoidAsync("GestioneCassaObj.updateDataOra", adesso.ToString("HH:mm:ss"));
        }
      }
      catch (TaskCanceledException tEx) { _ = tEx; }
      catch (Exception ex)
      {
        Log.Error(ex, $"{clientInfo.IPAddress} - Code Exception");
      }
    }
    private async void OnNotifyStatoProdotti(object sender, DatiNotifyStatoProdotti datiNotifyStatoProdotti)
    {
      try
      {
        AggiornaDatiCassa(datiNotifyStatoProdotti.statoProdotti);

        if (Module is not null)
        {
          await Module.InvokeVoidAsync("GestioneCassaObj.updateStatoProdotti", TabellaProdotti);
        }
      }
      catch (TaskCanceledException tEx) { _ = tEx; }
      catch (Exception ex)
      {
        Log.Error(ex, $"{clientInfo.IPAddress} - Code Exception");
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
        TabellaProdotti = _UserInterfaceService.AnagrProdotti.Values.Where(w => Cassa.prodottiVisibili.Contains(w.IdProdotto)).ToList();

        if (Module is not null)
        {
          await Module.InvokeVoidAsync("GestioneCassaObj.updateAnagrProdotti", IdCassa, TabellaProdotti);
        }
      }
      catch (TaskCanceledException tEx) { _ = tEx; }
      catch (Exception ex)
      {
        Log.Error(ex, $"{clientInfo.IPAddress} - Code Exception");
      }
    }
    #endregion
  }
}




