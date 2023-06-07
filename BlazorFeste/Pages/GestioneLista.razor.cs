using BlazorFeste.Data.Models;
using BlazorFeste.DataAccess;
using BlazorFeste.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Serilog;
using System.Text;

namespace BlazorFeste.Pages
{
  public partial class GestioneLista : IDisposable
  {
    [Parameter]
    public int IdLista { get; set; }

    #region Inject
    [Inject] public ClientInformationService _clientInfo { get; init; }
    [Inject] public UserInterfaceService _UserInterfaceService { get; init; }
    [Inject] public FesteDataAccess festeDataAccess { get; init; }
    [Inject] public IJSRuntime JSRuntime { get; init; }
    #endregion

    #region Variabili
    private IJSObjectReference Module;
    private Task<IJSObjectReference> _jsModule;
    private Task<IJSObjectReference> JsModule => _jsModule ??= JSRuntime.InvokeAsync<IJSObjectReference>("import", "./js/GestioneListaObj.js").AsTask();

    CancellationTokenSource cts = new();
    CancellationToken ct { get => cts.Token; }

    private DotNetObjectReference<GestioneLista> objRef;

    bool flagInizializza { get; set; } = false;
    AnagrListe Lista { get; set; } = new AnagrListe();
    List<AnagrProdotti> Prodotti { get; set; } = new List<AnagrProdotti>();
    public int ProdottiInLista { get; set; }

    private CamelCasePropertyNamesContractResolver contractResolver = new CamelCasePropertyNamesContractResolver { NamingStrategy = new CamelCaseNamingStrategy { } };
    #endregion

    #region LifeCycle
    protected override async Task OnInitializedAsync()
    {
      _UserInterfaceService.NotifyStatoOrdine += OnNotifyStatoOrdine;
      _UserInterfaceService.NotifyStatoLista += OnNotifyStatoLista;

      await base.OnInitializedAsync();
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

        await Module.InvokeVoidAsync("GestioneListaObj.init", objRef, Lista, Prodotti);
      }
      await base.OnAfterRenderAsync(firstRender);
    }
    protected override async Task OnParametersSetAsync()
    {
      Lista = _UserInterfaceService.AnagrListe.Where(w => w.IdLista == IdLista).FirstOrDefault();
#if THREADSAFE
      Prodotti = (_UserInterfaceService.AnagrProdotti.Values.Where(w => w.IdLista == IdLista)).ToList();
#else
      Prodotti = (_UserInterfaceService.AnagrProdotti.Where(w => w.IdLista == IdLista)).ToList();
#endif
      ProdottiInLista = Prodotti.Count();

      flagInizializza = true;

      await base.OnParametersSetAsync();
    }
    public void Dispose()
    {
      _UserInterfaceService.NotifyStatoOrdine -= OnNotifyStatoOrdine;
      _UserInterfaceService.NotifyStatoLista -= OnNotifyStatoLista;

      _jsModule?.Result.InvokeVoidAsync("GestioneListaObj.dispose");
      objRef?.Dispose();
    }
    #endregion

    #region Variabili
    #endregion

    #region Eventi
    #endregion

    #region Metodi
    public string CreaRigheHTML(ArchOrdini ordine)
    {
      StringBuilder sb = new StringBuilder();

#if THREADSAFE
      var righe = _UserInterfaceService.QryOrdiniRighe.Where(w => w.Key.Item1 == ordine.IdOrdine).OrderBy(o => o.Value.IdCategoria).ThenBy(o => o.Value.IdProdotto).ToList();
#else
      var righe = _UserInterfaceService.QryOrdiniRighe.Where(w => w.IdOrdine == ordine.IdOrdine).OrderBy(o => o.IdCategoria).ThenBy(o => o.IdProdotto).ToList();
#endif

      sb.Append("<div class='mb-1'><b>Composizione Ordine</b></div>");
      sb.Append("<table class='scontrino' style='width:100%;font-size:13px;' >");
      sb.Append(" <tr style='height:24px;font-size:10px;'>");
      sb.Append("   <td><b>Prodotto</b></td>");
      sb.Append("   <td style='text-align:center'><b>Qtà</b></td>");
      sb.Append("   <td style='text-align:center'><b>Importo</b></td>");
      sb.Append("   <td style='text-align:center'><b>In Carico</b></td>");
      sb.Append("   <td style='text-align:center'><b>Evasa</b></td>");
      sb.Append(" </tr>");
      sb.Append(" <tr style='height:1px; background-color: transparent'>");
      sb.Append("   <td colspan='5'></td>");
      sb.Append(" </tr>");

      if (righe.Count > 0)
      {
#if THREADSAFE
        int IdCategoriaMemo = righe[0].Value.IdCategoria;
        int QuantitàProdotto = 0;
        double Importo = 0.0;
        foreach (var Riga in righe)
        {
          QuantitàProdotto += Riga.Value.QuantitàProdotto;
          Importo += Riga.Value.Importo;

          if (IdCategoriaMemo != Riga.Value.IdCategoria)
          {
            IdCategoriaMemo = Riga.Value.IdCategoria;
            sb.Append("  <tr style='height:1px; background-color:transparent'>");
            sb.Append("    <td colspan='5'></td>");
            sb.Append("  </tr>");
          }

          string strBackColor;
          string strForeColor = "white";
          if (IdLista == 0)
          {
            strBackColor = "#d9d9d9";
          }
          else
          {
            switch (Riga.Value.IdStatoRiga)
            {
              case -1:
              case 0: strBackColor = "#dc3545"; strForeColor = "white"; break;   // Rosso
              case 1: strBackColor = "#ffc107"; strForeColor = "black"; break;   // Giallo
              case 2: strBackColor = "#28a745"; strForeColor = "white"; break;   // Verde scuro
              case 3: strBackColor = "#00cc00"; strForeColor = "black"; break;   // Verde Chiaro
              default: strBackColor = "#FF00FF"; strForeColor = "black"; break;  // Magenta
            };
          }
          sb.Append(string.Format(" <tr style='background-color:{0}; color: {1}'>", strBackColor, strForeColor));
          sb.Append(string.Format("   <td style='text-align:left'>{0}</td>", Riga.Value.NomeProdotto));
          sb.Append(string.Format("   <td style='text-align:center'>{0}</td>", Riga.Value.QuantitàProdotto));
          sb.Append(string.Format("   <td style='text-align:right'>{0:#.00 €}</td>", Riga.Value.Importo));
          sb.Append(string.Format("   <td style='text-align:center'>{0}</td>", Riga.Value.DataOra_RigaPresaInCarico.Ticks == 0 ? "-" : Riga.Value.DataOra_RigaPresaInCarico.ToString("H:mm:ss")));
          sb.Append(string.Format("   <td style='text-align:center'>{0}</td>", Riga.Value.DataOra_RigaEvasa.Ticks == 0 ? "-" : Riga.Value.DataOra_RigaEvasa.ToString("H:mm:ss")));
          sb.Append(" </tr>");
        }
        sb.Append("  <tr style='height:1px; background-color:transparent'>");
        sb.Append("    <td colspan='5'></td>");
        sb.Append("  </tr>");

        sb.Append(" <tr style='height:24px'>");
        sb.Append(string.Format("   <td><strong>{0}</strong></td>", "Totale"));
        sb.Append(string.Format("   <td style='text-align:center'>{0}</td>", QuantitàProdotto));
        sb.Append(string.Format("   <td style='text-align:right'>{0:#.00 €}</td>", Importo));
        sb.Append(string.Format("   <td>{0}</td>", ""));
        sb.Append(string.Format("   <td>{0}</td>", ""));
        sb.Append(" </tr>");
#else
        int IdCategoriaMemo = righe[0].IdCategoria;
        int QuantitàProdotto = 0;
        double Importo = 0.0;
        foreach (var Riga in righe)
        {
          QuantitàProdotto += Riga.QuantitàProdotto;
          Importo += Riga.Importo;

          if (IdCategoriaMemo != Riga.IdCategoria)
          {
            IdCategoriaMemo = Riga.IdCategoria;
            sb.Append("  <tr style='height:1px; background-color:transparent'>");
            sb.Append("    <td colspan='5'></td>");
            sb.Append("  </tr>");
          }

          string strBackColor;
          string strForeColor = "white";
          if (IdLista == 0)
          {
            strBackColor = "#d9d9d9";
          }
          else
          {
            switch (Riga.IdStatoRiga)
            {
              case -1:
              case 0: strBackColor = "#dc3545"; strForeColor = "white"; break;   // Rosso
              case 1: strBackColor = "#ffc107"; strForeColor = "black"; break;   // Giallo
              case 2: strBackColor = "#28a745"; strForeColor = "white"; break;   // Verde scuro
              case 3: strBackColor = "#00cc00"; strForeColor = "black"; break;   // Verde Chiaro
              default: strBackColor = "#FF00FF"; strForeColor = "black"; break;  // Magenta
            };
          }
          sb.Append(string.Format(" <tr style='background-color:{0}; color: {1}'>", strBackColor, strForeColor));
          sb.Append(string.Format("   <td style='text-align:left'>{0}</td>", Riga.NomeProdotto));
          sb.Append(string.Format("   <td style='text-align:center'>{0}</td>", Riga.QuantitàProdotto));
          sb.Append(string.Format("   <td style='text-align:right'>{0:#.00 €}</td>", Riga.Importo));
          sb.Append(string.Format("   <td style='text-align:center'>{0}</td>", Riga.DataOra_RigaPresaInCarico.Ticks == 0 ? "-" : Riga.DataOra_RigaPresaInCarico.ToString("H:mm:ss")));
          sb.Append(string.Format("   <td style='text-align:center'>{0}</td>", Riga.DataOra_RigaEvasa.Ticks == 0 ? "-" : Riga.DataOra_RigaEvasa.ToString("H:mm:ss")));
          sb.Append(" </tr>");
        }
        sb.Append("  <tr style='height:1px; background-color:transparent'>");
        sb.Append("    <td colspan='5'></td>");
        sb.Append("  </tr>");

        sb.Append(" <tr style='height:24px'>");
        sb.Append(string.Format("   <td><strong>{0}</strong></td>", "Totale"));
        sb.Append(string.Format("   <td style='text-align:center'>{0}</td>", QuantitàProdotto));
        sb.Append(string.Format("   <td style='text-align:right'>{0:#.00 €}</td>", Importo));
        sb.Append(string.Format("   <td>{0}</td>", ""));
        sb.Append(string.Format("   <td>{0}</td>", ""));
        sb.Append(" </tr>");
#endif
      }
      sb.Append("</table>");

      // Return the result.
      return sb.ToString();
    }
    async void OnNotifyStatoOrdine(object sender, long idOrdine)
    {
      try
      {
#if THREADSAFE
        var DatiOrdine = from o in _UserInterfaceService.QryOrdini.Where(w => w.Key == idOrdine)
                         join r in _UserInterfaceService.QryOrdiniRighe on o.Key equals r.Key.Item1
                         join p in _UserInterfaceService.AnagrProdotti.Values.Where(w => w.IdLista == Lista.IdLista) on r.Value.IdProdotto equals p.IdProdotto
                         select new { o, r, p };
#else
        var DatiOrdine = from o in _UserInterfaceService.QryOrdini.Where(w => w.IdOrdine == idOrdine)
                         join r in _UserInterfaceService.QryOrdiniRighe on o.IdOrdine equals r.IdOrdine
                         join p in _UserInterfaceService.AnagrProdotti.Where(w => w.IdLista == Lista.IdLista) on r.IdProdotto equals p.IdProdotto
                         select new { o, r, p };
#endif

        if (DatiOrdine.Any())
        {
          // L'ordine ha qualcosa relativo alla lista visualizzata
          //Log.Information($"NotifyStatoOrdine - Mi interessa!");
          await Module.InvokeVoidAsync("GestioneListaObj.btnRefresh");
          //await Module.InvokeVoidAsync("GestioneListaObj.onNotifyStatoOrdine", idOrdine); - TODO
        }
      }
      catch (Exception ex)
      {
        Log.Error($"{_clientInfo.IPAddress} - OnNotifyStatoOrdine - {ex.Message}");
      }
    }
    void OnNotifyStatoLista(object sender, int idLista)
    {
      if (Lista.IdLista == idLista)
      {
        // E' stata aggiornata la lista che stò visualizzzando
        // Dovrei scoprire come fare per sapere che non sono stato io a farlo
      }
    }

#endregion

#region JSInvokable
    [JSInvokable("RefreshGridOrdiniRowsAsync")]
    public async Task<List<ListaOrdini>> RefreshGridOrdiniRowsAsync(bool _Filtro_StatoRiga)
    {
      //Log.Information($"RefreshGridOrdiniRowsAsync - Enter");
      var watch = System.Diagnostics.Stopwatch.StartNew();

      List<int> StatoRiga = new List<int>();
      StatoRiga = _Filtro_StatoRiga switch
      {
        false => Enumerable.Range(-1, 4).ToList(),  // -1, 0, 1, 2
        _ => Enumerable.Range(3, 2).ToList(),       // 3, 4
      };

      // Recupero tutte le righe della serata in corso, della lista selezionata
#if THREADSAFE
      var RigheOrdini_SerataInCorso = (from o in _UserInterfaceService.QryOrdini.Where(w => w.Value.IdStatoOrdine >= Lista.Priorità)
                                       join r in _UserInterfaceService.QryOrdiniRighe.Where(w => StatoRiga.Contains(w.Value.IdStatoRiga)) on o.Key equals r.Key.Item1
                                       join p in Prodotti on r.Value.IdProdotto equals p.IdProdotto
                                       select new
                                       {
                                         p.Ordine,
                                         r.Value.IdOrdine,
                                         r.Value.IdProdotto,
                                         r.Value.IdStatoRiga,
                                         r.Value.QuantitàProdotto,
                                         r = r.Value
                                       }).ToList();

#else
      var RigheOrdini_SerataInCorso = (from o in _UserInterfaceService.QryOrdini.Where(w => w.IdStatoOrdine >= Lista.Priorità)
                                       join r in _UserInterfaceService.QryOrdiniRighe.Where(w => StatoRiga.Contains(w.IdStatoRiga)) on o.IdOrdine equals r.IdOrdine
                                       join p in Prodotti on r.IdProdotto equals p.IdProdotto
                                       select new
                                       {
                                         p.Ordine,
                                         r.IdOrdine,
                                         r.IdProdotto,
                                         r.IdStatoRiga,
                                         r.QuantitàProdotto,
                                         r
                                       }).ToList();

#endif

      List<long> OrdiniConRighe = new List<long>();
      foreach (var itemR in RigheOrdini_SerataInCorso)
      {
        if (OrdiniConRighe.Contains(itemR.IdOrdine) == false)
        {
          OrdiniConRighe.Add(itemR.IdOrdine);
        }
      }

#if THREADSAFE
      List<ListaOrdini> _Ordini = (from o in _UserInterfaceService.QryOrdini.Where(w => (OrdiniConRighe.Contains(w.Key)) && (w.Value.IdStatoOrdine >= Lista.Priorità)).OrderBy(o => o.Value.Timestamp)
                                   select new ListaOrdini
                                   {
                                     IdOrdine = o.Key,
                                     ProgressivoSerata = o.Value.ProgressivoSerata,
                                     IdStatoOrdine = o.Value.IdStatoOrdine,
                                     Cassa = o.Value.Cassa,
                                     DataOra = o.Value.DataOra.ToString("HH:mm:ss"),
                                     Tavolo = o.Value.TipoOrdine.CompareTo("SERVITO") == 0 ? o.Value.Tavolo : o.Value.TipoOrdine,
                                     NumeroCoperti = o.Value.NumeroCoperti,
                                     Referente = o.Value.Referente,
                                     NoteOrdine = o.Value.NoteOrdine, 
                                     Righe = new ArchOrdiniRighe[ProdottiInLista],
                                     RigheHTML = CreaRigheHTML(o.Value),
                                     Priorità = Lista.Priorità,
                                     StatoRighe = new int[] { 0, 0, 0, 0 }
                                   }).ToList();
#else
      List<ListaOrdini> _Ordini = (from o in _UserInterfaceService.QryOrdini.Where(w => (OrdiniConRighe.Contains(w.IdOrdine)) && (w.IdStatoOrdine >= Lista.Priorità)).OrderBy(o => o.Timestamp)
                                   select new ListaOrdini
                                   {
                                     IdOrdine = o.IdOrdine,
                                     ProgressivoSerata = o.ProgressivoSerata,
                                     IdStatoOrdine = o.IdStatoOrdine,
                                     Cassa = o.Cassa,
                                     DataOra = o.DataOra.ToString("HH:mm:ss"),
                                     Tavolo = o.TipoOrdine.CompareTo("SERVITO") == 0 ? o.Tavolo : o.TipoOrdine,
                                     NumeroCoperti = o.NumeroCoperti,
                                     Referente = o.Referente,
                                     //Asterisco = o.NoteOrdine.Length > 0 ? "(*)" : "",
                                     NoteOrdine = o.NoteOrdine, //.Replace("'", " "),
                                     Righe = new ArchOrdiniRighe[ProdottiInLista],
                                     RigheHTML = CreaRigheHTML(o),
                                     Priorità = Lista.Priorità,
                                     StatoRighe = new int[] { 0, 0, 0, 0 }
                                   }).ToList();
#endif
      foreach (var itemR in RigheOrdini_SerataInCorso)
      {
        int idxOrdine = _Ordini.FindIndex(i => i.IdOrdine == itemR.IdOrdine);
        if (itemR.Ordine > 0)
        {
          _Ordini[idxOrdine].Righe[itemR.Ordine - 1] = itemR.r;
          _Ordini[idxOrdine].StatoRighe[itemR.r.IdStatoRiga]++;
        }
      }

      await InvokeAsync(StateHasChanged);

      watch.Stop();
      //Log.Information($"{_clientInfo.IPAddress} - RefreshGridOrdiniRowsAsync - {watch.ElapsedMilliseconds} msec");

      //var aaa = _Ordini.OrderByDescending(o => o.IdOrdine).Take(48).ToList();
      
      if (_Filtro_StatoRiga)
      {
        return (_Ordini.OrderByDescending(o => o.IdOrdine).ToList());
      }
      else
      {
        return (_Ordini.OrderBy(o => o.IdOrdine).Take(40).ToList());
      }
    }
    [JSInvokable("RefreshGridOrdiniHeaderAsync")]
    public string RefreshGridOrdiniHeaderAsync()
    {
      //Log.Information($"RefreshGridOrdiniHeaderAsync - Enter");
      var watch = System.Diagnostics.Stopwatch.StartNew();

#if THREADSAFE
      var RigheOrdini_SerataInCorso = (from o in _UserInterfaceService.QryOrdini
                                       join r in _UserInterfaceService.QryOrdiniRighe on o.Key equals r.Key.Item1
                                       join p in Prodotti.Where(w => w.IdLista == Lista.IdLista) on r.Value.IdProdotto equals p.IdProdotto
                                       group r by new { r.Value.IdStatoRiga, p.Ordine } into g
                                       select new
                                       {
                                         g.Key.IdStatoRiga,
                                         g.Key.Ordine,
                                         SommaDiQuantitàProdotto = g.Sum(s => s.Value.QuantitàProdotto),
                                         SommaDiQuantitàEvasa = g.Sum(s => s.Value.QuantitàEvasa)
                                       }).ToList();
#else
      var RigheOrdini_SerataInCorso = (from o in _UserInterfaceService.QryOrdini
                                       join r in _UserInterfaceService.QryOrdiniRighe on o.IdOrdine equals r.IdOrdine
                                       join p in Prodotti.Where(w => w.IdLista == Lista.IdLista) on r.IdProdotto equals p.IdProdotto
                                       group r by new { r.IdStatoRiga, p.Ordine } into g
                                       select new
                                       {
                                         g.Key.IdStatoRiga,
                                         g.Key.Ordine,
                                         SommaDiQuantitàProdotto = g.Sum(s => s.QuantitàProdotto),
                                         SommaDiQuantitàEvasa = g.Sum(s => s.QuantitàEvasa)
                                       }).ToList();
#endif
      int[,] RigaTotali = new int[4, ProdottiInLista];
      foreach (var itemR in RigheOrdini_SerataInCorso)
      {
        switch (itemR.IdStatoRiga)
        {
          case -1:
          case 0:
            RigaTotali[0, itemR.Ordine - 1] = RigaTotali[0, itemR.Ordine - 1] + itemR.SommaDiQuantitàProdotto - itemR.SommaDiQuantitàEvasa;
            break;

          default:
            RigaTotali[itemR.IdStatoRiga, itemR.Ordine - 1] = RigaTotali[itemR.IdStatoRiga, itemR.Ordine - 1] + itemR.SommaDiQuantitàProdotto - itemR.SommaDiQuantitàEvasa;
            break;
        }
      }
      watch.Stop();
      //Log.Information($"{_clientInfo.IPAddress} - RefreshGridOrdiniHeaderAsync - {watch.ElapsedMilliseconds} msec");

      return (JsonConvert.SerializeObject(RigaTotali));
    }
    [JSInvokable("SbloccaListeAsync")]
    public async Task SbloccaListeAsync(int IdOrdine)
    {
#if THREADSAFE
      ArchOrdini Ordine = _UserInterfaceService.QryOrdini.Where(w => w.Key == IdOrdine).Select(s => s.Value).FirstOrDefault();
      ArchOrdini OrdinePre = Ordine;
#else
      ArchOrdini Ordine = _UserInterfaceService.QryOrdini.Where(w => w.IdOrdine == IdOrdine).FirstOrDefault();
#endif
      Ordine.IdStatoOrdine = 2;

      await festeDataAccess.UpdateArchOrdiniAsync(Ordine);

#if THREADSAFE
      // Aggiorno il record dell'Ordine nella struttura di memoria
      var index = _UserInterfaceService.QryOrdini.TryUpdate(Ordine.IdOrdine, Ordine, OrdinePre);

      // Tutte le righe dell'ordine che fanno parte delle liste figlio devo essere riconoscibili - Metto il loro IdStatoRiga a -1
      var ListeFiglio = _UserInterfaceService.AnagrListe.Where(w => w.IdListaPadre == Lista.IdLista).Select(s => s.IdLista).ToArray();
      var RigheDelleListeFiglio = (from r in _UserInterfaceService.QryOrdiniRighe.Where(w => (w.Key.Item1 == IdOrdine))
                                   join p in Prodotti.Where(w => ListeFiglio.Contains(w.IdLista)) on r.Value.IdProdotto equals p.IdProdotto
                                   select r.Value.IdRiga).ToArray();

      foreach (var item in _UserInterfaceService.QryOrdiniRighe.Where(w => (w.Key.Item1 == IdOrdine) && RigheDelleListeFiglio.Contains(w.Value.IdRiga)))
      {
        item.Value.IdStatoRiga = -1;

        await festeDataAccess.UpdateArchOrdiniRigheAsync(item.Value);
      }
#else
      // Aggiorno il record dell'Ordine nella struttura di memoria
      var index = _UserInterfaceService.QryOrdini.FindIndex(c => (c.IdOrdine == Ordine.IdOrdine));
      _UserInterfaceService.QryOrdini[index] = Ordine;
      // Tutte le righe dell'ordine che fanno parte delle liste figlio devo essere riconoscibili - Metto il loro IdStatoRiga a -1
      var ListeFiglio = _UserInterfaceService.AnagrListe.Where(w => w.IdListaPadre == Lista.IdLista).Select(s => s.IdLista).ToArray();

      var RigheDelleListeFiglio = (from r in _UserInterfaceService.QryOrdiniRighe.Where(w => (w.IdOrdine == IdOrdine))
                                   join p in Prodotti.Where(w => ListeFiglio.Contains(w.IdLista)) on r.IdProdotto equals p.IdProdotto
                                   select r.IdRiga).ToArray();

      foreach (var item in _UserInterfaceService.QryOrdiniRighe.Where(w => (w.IdOrdine == IdOrdine) && RigheDelleListeFiglio.Contains(w.IdRiga)))
      {
        item.IdStatoRiga = -1;

        await festeDataAccess.UpdateArchOrdiniRigheAsync(item);
      }
#endif

      _UserInterfaceService.OnNotifyStatoOrdine(IdOrdine);
    }
    [JSInvokable("AggiornaDatiOrdineAsync")]
    public async Task<ArchOrdini> AggiornaDatiOrdineAsync(ListaOrdini _ordine)
    {
#if THREADSAFE
      ArchOrdini Ordine = _UserInterfaceService.QryOrdini.Where(w => w.Key == _ordine.IdOrdine).Select(s => s.Value).FirstOrDefault();
      ArchOrdini OrdinePre = Ordine;
#else
      ArchOrdini Ordine = _UserInterfaceService.QryOrdini.Where(w => w.IdOrdine == _ordine.IdOrdine).FirstOrDefault();
      ArchOrdini OrdinePre = Ordine;
#endif

      var isTavoloNumeric = int.TryParse(_ordine.Tavolo, out int n);
      if (isTavoloNumeric && Ordine.TipoOrdine != "SERVITO")
      {
        Ordine.TipoOrdine = "SERVITO";
      }
      Ordine.Tavolo         = _ordine.Tavolo;
      Ordine.NoteOrdine     = _ordine.NoteOrdine;
      Ordine.NumeroCoperti  = _ordine.NumeroCoperti;
      Ordine.Referente      = _ordine.Referente;
      
      await festeDataAccess.UpdateArchOrdiniAsync(Ordine);

#if THREADSAFE
      // Aggiorno il record dell'Ordine nella struttura di memoria
      var index = _UserInterfaceService.QryOrdini.TryUpdate(Ordine.IdOrdine, Ordine, OrdinePre);
#else
      // Aggiorno il record dell'Ordine nella struttura di memoria
      var index = _UserInterfaceService.QryOrdini.FindIndex(c => (c.IdOrdine == Ordine.IdOrdine));
      _UserInterfaceService.QryOrdini[index] = Ordine;
#endif
      // Se arrivo qui significa che :
      //    Devo notificare le modifiche a chi lo desidera 
      _UserInterfaceService.OnNotifyStatoOrdine(Ordine.IdOrdine);

      return (Ordine);
    }

    [JSInvokable("AggiornaStatoRigaAsync")]
    public async Task<string> AggiornaStatoRigaAsync(bool _Filtro_StatoRiga, ArchOrdiniRighe Riga, int idStatoRigaNew)
    {
      int idStatoRigaOld = Riga.IdStatoRiga;

#if THREADSAFE
      ArchOrdiniRighe res = _UserInterfaceService.QryOrdiniRighe.Where(w => w.Key.Item1 == Riga.IdOrdine && w.Key.Item2 == Riga.IdRiga).Select(s => s.Value).FirstOrDefault();
      ArchOrdiniRighe resPre = res;
#else
      ArchOrdiniRighe res = _UserInterfaceService.QryOrdiniRighe.Where(w => w.IdOrdine == Riga.IdOrdine && w.IdRiga == Riga.IdRiga).FirstOrDefault();
      ArchOrdiniRighe resPre = res;
#endif
      res.IdStatoRiga = idStatoRigaNew;

      switch (idStatoRigaNew)
      {
        case 1:
          // Presa in carico
          if (idStatoRigaOld <= 0) res.DataOra_RigaPresaInCarico = DateTime.Now;
          break;
        case 2:
          // Evasa
          res.QuantitàEvasa = res.QuantitàProdotto;
          res.DataOra_RigaEvasa = DateTime.Now;
          break;
        default:
          break;
      }
      await festeDataAccess.UpdateArchOrdiniRigheAsync(res);

      List<int> StatoRiga = new List<int>();
      StatoRiga = _Filtro_StatoRiga switch
      {
        false => Enumerable.Range(-1, 4).ToList(),  // -1, 0, 1, 2
        _ => Enumerable.Range(3, 2).ToList(),       // 3, 4
      };

#if THREADSAFE
      // Aggiorno il record dell'Ordine nella struttura di memoria
      var index = _UserInterfaceService.QryOrdiniRighe.TryUpdate(new Tuple<long, int>(res.IdOrdine, res.IdRiga), res, resPre);

      var RigheOrdine = (from r in _UserInterfaceService.QryOrdiniRighe.Where(w => (w.Key.Item1 == Riga.IdOrdine) && StatoRiga.Contains(w.Value.IdStatoRiga))
                         join p in Prodotti on r.Value.IdProdotto equals p.IdProdotto
                         select new
                         {
                           p.Ordine,
                           r = r.Value
                         }).ToList();
#else
      // Aggiorno il record dell'Ordine nella struttura di memoria
      var index = _UserInterfaceService.QryOrdiniRighe.FindIndex(c => (c.IdOrdine == res.IdOrdine) && (c.IdRiga == res.IdRiga));
      _UserInterfaceService.QryOrdiniRighe[index] = res;

      var RigheOrdine = (from r in _UserInterfaceService.QryOrdiniRighe.Where(w => (w.IdOrdine == Riga.IdOrdine) && StatoRiga.Contains(w.IdStatoRiga))
                         join p in Prodotti on r.IdProdotto equals p.IdProdotto
                         select new
                         {
                           p.Ordine,
                           r
                         }).ToList();
#endif
      ArchOrdiniRighe[] righe = new ArchOrdiniRighe[ProdottiInLista];

      int[] StatoRighe = new int[] { 0, 0, 0, 0 };
      foreach (var itemR in RigheOrdine)
      {
        righe[itemR.Ordine - 1] = itemR.r;
        StatoRighe[itemR.r.IdStatoRiga]++;
      }

      Dictionary<string, object> sendDict = new Dictionary<string, object>();
      sendDict.Add("righe", righe);
      sendDict.Add("stato", StatoRighe);
      
      return(JsonConvert.SerializeObject(sendDict, new JsonSerializerSettings { ContractResolver = contractResolver }));

      //return (righe); // JsonConvert.SerializeObject(new { id, righe }, new JsonSerializerSettings { ContractResolver = contractResolver }));
    }
    [JSInvokable("AggiornaQuantitàEvasaAsync")]
    public async Task<string> AggiornaQuantitàEvasaAsync(bool _Filtro_StatoRiga, ArchOrdiniRighe Riga, int QuantitàEvasaNew)
    {
#if THREADSAFE
      ArchOrdiniRighe res = _UserInterfaceService.QryOrdiniRighe.Where(w => w.Key.Item1 == Riga.IdOrdine && w.Key.Item2 == Riga.IdRiga).Select(s => s.Value).FirstOrDefault();
      ArchOrdiniRighe resPre = res;
#else
      ArchOrdiniRighe res = _UserInterfaceService.QryOrdiniRighe.Where(w => w.IdOrdine == Riga.IdOrdine && w.IdRiga == Riga.IdRiga).FirstOrDefault();
#endif

      res.QuantitàEvasa = QuantitàEvasaNew;
      await festeDataAccess.UpdateArchOrdiniRigheAsync(res);

      List<int> StatoRiga = new List<int>();
      StatoRiga = _Filtro_StatoRiga switch
      {
        false => Enumerable.Range(-1, 4).ToList(),  // -1, 0, 1, 2
        _ => Enumerable.Range(3, 2).ToList(),       // 3, 4
      };

#if THREADSAFE
      // Aggiorno il record dell'Ordine nella struttura di memoria
      var index = _UserInterfaceService.QryOrdiniRighe.TryUpdate(new Tuple<long, int>(res.IdOrdine, res.IdRiga), res, resPre);

      var RigheOrdine = (from r in _UserInterfaceService.QryOrdiniRighe.Where(w => (w.Key.Item1 == Riga.IdOrdine) && StatoRiga.Contains(w.Value.IdStatoRiga))
                         join p in Prodotti on r.Value.IdProdotto equals p.IdProdotto
                         select new
                         {
                           p.Ordine,
                           r = r.Value
                         }).ToList();
#else
      // Aggiorno il record dell'Ordine nella struttura di memoria
      var index = _UserInterfaceService.QryOrdiniRighe.FindIndex(c => (c.IdOrdine == res.IdOrdine) && (c.IdRiga == res.IdRiga));
      _UserInterfaceService.QryOrdiniRighe[index] = res;

      var RigheOrdine = (from r in _UserInterfaceService.QryOrdiniRighe.Where(w => (w.IdOrdine == Riga.IdOrdine) && StatoRiga.Contains(w.IdStatoRiga))
                         join p in Prodotti on r.IdProdotto equals p.IdProdotto
                         select new
                         {
                           p.Ordine,
                           r
                         }).ToList();
#endif
      ArchOrdiniRighe[] righe = new ArchOrdiniRighe[ProdottiInLista];

      int[] StatoRighe = new int[] { 0, 0, 0, 0 };
      foreach (var itemR in RigheOrdine)
      {
        righe[itemR.Ordine - 1] = itemR.r;
        StatoRighe[itemR.r.IdStatoRiga]++;
      }

      Dictionary<string, object> sendDict = new Dictionary<string, object>();
      sendDict.Add("righe", righe);
      sendDict.Add("stato", StatoRighe);

      return (JsonConvert.SerializeObject(sendDict, new JsonSerializerSettings { ContractResolver = contractResolver }));

      //return (righe);
    }
    [JSInvokable("PrendiInCaricoOrdineAsync")]
    public async Task<string> PrendiInCaricoOrdineAsync(bool _Filtro_StatoRiga, int IdOrdine)
    {
      // Tutte le righe che sono ancora nello stato 0 oppure -1 vengono messe nello stato 1
      var Prodotto = Prodotti.Select(s => s.IdProdotto).ToArray();

      List<int> StatoRiga = new List<int>();
      StatoRiga = _Filtro_StatoRiga switch
      {
        false => Enumerable.Range(-1, 4).ToList(),  // -1, 0, 1, 2
        _ => Enumerable.Range(3, 2).ToList(),       // 3, 4
      };

#if THREADSAFE
      foreach (var item in _UserInterfaceService.QryOrdiniRighe.Where(w => (w.Key.Item1 == IdOrdine) && Prodotto.Contains(w.Value.IdProdotto) && (w.Value.IdStatoRiga <= 0)))
      {
        item.Value.IdStatoRiga = 1;
        item.Value.DataOra_RigaPresaInCarico = DateTime.Now;

        await festeDataAccess.UpdateArchOrdiniRigheAsync(item.Value);
      }

      var RigheOrdine = (from r in _UserInterfaceService.QryOrdiniRighe.Where(w => (w.Key.Item1 == IdOrdine) && StatoRiga.Contains(w.Value.IdStatoRiga))
                         join p in Prodotti on r.Value.IdProdotto equals p.IdProdotto
                         select new
                         {
                           p.Ordine,
                           r = r.Value
                         }).ToList();
#else
      foreach (var item in _UserInterfaceService.QryOrdiniRighe.Where(w => (w.IdOrdine == IdOrdine) && Prodotto.Contains(w.IdProdotto) && (w.IdStatoRiga <= 0)))
      {
        item.IdStatoRiga = 1;
        item.DataOra_RigaPresaInCarico = DateTime.Now;

        await festeDataAccess.UpdateArchOrdiniRigheAsync(item);
      }

      var RigheOrdine = (from r in _UserInterfaceService.QryOrdiniRighe.Where(w => (w.IdOrdine == IdOrdine) && StatoRiga.Contains(w.IdStatoRiga))
                         join p in Prodotti on r.IdProdotto equals p.IdProdotto
                         select new
                         {
                           p.Ordine,
                           r
                         }).ToList();
#endif
      ArchOrdiniRighe[] righe = new ArchOrdiniRighe[ProdottiInLista];

      int[] StatoRighe = new int[] { 0, 0, 0, 0 };
      foreach (var itemR in RigheOrdine)
      {
        righe[itemR.Ordine - 1] = itemR.r;
        StatoRighe[itemR.r.IdStatoRiga]++;
      }

      Dictionary<string, object> sendDict = new Dictionary<string, object>();
      sendDict.Add("righe", righe);
      sendDict.Add("stato", StatoRighe);

      return (JsonConvert.SerializeObject(sendDict, new JsonSerializerSettings { ContractResolver = contractResolver }));
    }
    [JSInvokable("AggiornaStatoListaAsync")]
    public async Task<string> AggiornaStatoListaAsync(bool _Filtro_StatoRiga, long IdOrdine)
    {
      // Tutte le righe della liste vengono mese nello stato 2
      var Prodotto = Prodotti.Select(s => s.IdProdotto).ToArray();

      List<int> StatoRiga = new List<int>();
      StatoRiga = _Filtro_StatoRiga switch
      {
        false => Enumerable.Range(-1, 4).ToList(),  // -1, 0, 1, 2
        _ => Enumerable.Range(3, 2).ToList(),       // 3, 4
      };

#if THREADSAFE
      foreach (var item in _UserInterfaceService.QryOrdiniRighe.Where(w => (w.Key.Item1 == IdOrdine) && Prodotto.Contains(w.Value.IdProdotto)))
      {
        item.Value.IdStatoRiga = 2;

        await festeDataAccess.UpdateArchOrdiniRigheAsync(item.Value);
      }
      var Ordine = _UserInterfaceService.QryOrdini.Where(w => w.Key == IdOrdine).Select(s => s.Value).FirstOrDefault();

      var RigheOrdine = (from r in _UserInterfaceService.QryOrdiniRighe.Where(w => (w.Key.Item1 == IdOrdine) && StatoRiga.Contains(w.Value.IdStatoRiga))
                         join p in Prodotti on r.Value.IdProdotto equals p.IdProdotto
                         select new
                         {
                           p.Ordine,
                           r = r.Value
                         }).ToList();
#else
      foreach (var item in _UserInterfaceService.QryOrdiniRighe.Where(w => (w.IdOrdine == IdOrdine) && Prodotto.Contains(w.IdProdotto)))
      {
        item.IdStatoRiga = 2;

        await festeDataAccess.UpdateArchOrdiniRigheAsync(item);
      }
      var Ordine = _UserInterfaceService.QryOrdini.Where(w => w.IdOrdine == IdOrdine).FirstOrDefault();

      var RigheOrdine = (from r in _UserInterfaceService.QryOrdiniRighe.Where(w => (w.IdOrdine == IdOrdine) && StatoRiga.Contains(w.IdStatoRiga))
                         join p in Prodotti on r.IdProdotto equals p.IdProdotto
                         select new
                         {
                           p.Ordine,
                           r
                         }).ToList();
#endif
      ArchOrdiniRighe[] righe = new ArchOrdiniRighe[ProdottiInLista];

      int[] StatoRighe = new int[] { 0, 0, 0, 0 };
      foreach (var itemR in RigheOrdine)
      {
        righe[itemR.Ordine - 1] = itemR.r;
        StatoRighe[itemR.r.IdStatoRiga]++;
      }

      Dictionary<string, object> sendDict = new Dictionary<string, object>();
      sendDict.Add("righe", righe);
      sendDict.Add("stato", StatoRighe);

      return (JsonConvert.SerializeObject(sendDict, new JsonSerializerSettings { ContractResolver = contractResolver }));
    }
    [JSInvokable("EvadiListaAsync")]
    public async Task EvadiListaAsync(long IdOrdine)
    {
      // Tutte le righe della lista vengono messe nello stato 3
      var Prodotto = Prodotti.Select(s => s.IdProdotto).ToArray();

#if THREADSAFE
      foreach (var item in _UserInterfaceService.QryOrdiniRighe.Where(w => (w.Key.Item1 == IdOrdine) && Prodotto.Contains(w.Value.IdProdotto)))
      {
        item.Value.IdStatoRiga = 3;
        item.Value.QuantitàEvasa = item.Value.QuantitàProdotto;

        await festeDataAccess.UpdateArchOrdiniRigheAsync(item.Value);

        AnagrProdotti _anagrProdotto = _UserInterfaceService.AnagrProdotti.Values.Where(w => w.IdProdotto == item.Value.IdProdotto).FirstOrDefault();
        _anagrProdotto.Evaso += Convert.ToUInt32(item.Value.QuantitàEvasa);

        if (_anagrProdotto.IdProdotto != _anagrProdotto.EvadiSuIdProdotto)
        {
          // Devo aggiornare anche il prodotto dove tengo il conteggio cumulativo
          AnagrProdotti _anagrProdottoSuCuiEvadere = _UserInterfaceService.AnagrProdotti.Values.Where(w => w.IdProdotto == _anagrProdotto.EvadiSuIdProdotto).FirstOrDefault();
          _anagrProdottoSuCuiEvadere.EvasoCumulativo += Convert.ToUInt32(item.Value.QuantitàEvasa);
          await festeDataAccess.UpdateAnagrProdottiAsync(_anagrProdottoSuCuiEvadere);
        }
        else
        {
          _anagrProdotto.EvasoCumulativo += Convert.ToUInt32(item.Value.QuantitàEvasa);
        }
        await festeDataAccess.UpdateAnagrProdottiAsync(_anagrProdotto);
      }
      var Ordine = _UserInterfaceService.QryOrdini.Where(w => w.Key == IdOrdine).Select(s => s.Value).FirstOrDefault();
      var OrdinePre = Ordine;

      // Se la lista è una lista padre (IoSonoListaPadre = true)
      if (Lista.IoSonoListaPadre == true)
      {
        // Sblocco le liste figlio
        Ordine.IdStatoOrdine = 2;
        await festeDataAccess.UpdateArchOrdiniAsync(Ordine);

        // Aggiorno il record dell'Ordine nella struttura di memoria
        var index = _UserInterfaceService.QryOrdini.TryUpdate(Ordine.IdOrdine, Ordine, OrdinePre);

        // Tutte le righe dell'ordine che fanno parte delle liste figlio devo essere riconoscibili - Metto il loro IdStatoRiga a -1
        var ListeFiglio = _UserInterfaceService.AnagrListe.Where(w => w.IdListaPadre == Lista.IdLista).Select(s => s.IdLista).ToArray();
        var RigheDelleListeFiglio = (from r in _UserInterfaceService.QryOrdiniRighe.Where(w => (w.Key.Item1 == IdOrdine))
                                     join p in Prodotti.Where(w => ListeFiglio.Contains(w.IdLista)) on r.Value.IdProdotto equals p.IdProdotto
                                     select r.Value.IdRiga).ToArray();

        foreach (var item in _UserInterfaceService.QryOrdiniRighe.Where(w => (w.Key.Item1 == IdOrdine) && RigheDelleListeFiglio.Contains(w.Value.IdRiga)))
        {
          item.Value.IdStatoRiga = -1;

          await festeDataAccess.UpdateArchOrdiniRigheAsync(item.Value);
        }
      }
#else
      foreach (var item in _UserInterfaceService.QryOrdiniRighe.Where(w => (w.IdOrdine == IdOrdine) && Prodotto.Contains(w.IdProdotto)))
      {
        item.IdStatoRiga = 3;

        await festeDataAccess.UpdateArchOrdiniRigheAsync(item);

        AnagrProdotti _anagrProdotto = _UserInterfaceService.AnagrProdotti.Where(w => w.IdProdotto == item.IdProdotto).FirstOrDefault();
        _anagrProdotto.Evaso += Convert.ToUInt32(item.QuantitàEvasa);
        //_anagrProdotto.Evaso = res.QueueTicket + Convert.ToUInt32(res.QuantitàEvasa);

        if (_anagrProdotto.IdProdotto != _anagrProdotto.EvadiSuIdProdotto)
        {
          // Devo aggiornare anche il prodotto dove tengo il conteggio cumulativo
          AnagrProdotti _anagrProdottoSuCuiEvadere = _UserInterfaceService.AnagrProdotti.Where(w => w.IdProdotto == _anagrProdotto.EvadiSuIdProdotto).FirstOrDefault();
          _anagrProdottoSuCuiEvadere.EvasoCumulativo += Convert.ToUInt32(item.QuantitàEvasa);
          await festeDataAccess.UpdateAnagrProdottiAsync(_anagrProdottoSuCuiEvadere);
        }
        else
        {
          _anagrProdotto.EvasoCumulativo += Convert.ToUInt32(item.QuantitàEvasa);
        }
        await festeDataAccess.UpdateAnagrProdottiAsync(_anagrProdotto);
      }
      var Ordine = _UserInterfaceService.QryOrdini.Where(w => w.IdOrdine == IdOrdine).FirstOrDefault();

      // Se la lista è una lista padre (IoSonoListaPadre = true)
      if (Lista.IoSonoListaPadre == true)
      {
        // Sblocco le liste figlio
        Ordine.IdStatoOrdine = 2;
        await festeDataAccess.UpdateArchOrdiniAsync(Ordine);

        // Aggiorno il record dell'Ordine nella struttura di memoria
        var index = _UserInterfaceService.QryOrdini.FindIndex(c => (c.IdOrdine == Ordine.IdOrdine));
        _UserInterfaceService.QryOrdini[index] = Ordine;

        // Tutte le righe dell'ordine che fanno parte delle liste figlio devo essere riconoscibili - Metto il loro IdStatoRiga a -1
        var ListeFiglio = _UserInterfaceService.AnagrListe.Where(w => w.IdListaPadre == Lista.IdLista).Select(s => s.IdLista).ToArray();
        var RigheDelleListeFiglio = (from r in _UserInterfaceService.QryOrdiniRighe.Where(w => (w.IdOrdine == IdOrdine))
                                     join p in Prodotti.Where(w => ListeFiglio.Contains(w.IdLista)) on r.IdProdotto equals p.IdProdotto
                                     select r.IdRiga).ToArray();

        foreach (var item in _UserInterfaceService.QryOrdiniRighe.Where(w => (w.IdOrdine == IdOrdine) && RigheDelleListeFiglio.Contains(w.IdRiga)))
        {
          item.IdStatoRiga = -1;

          await festeDataAccess.UpdateArchOrdiniRigheAsync(item);
        }
      }
#endif
      _UserInterfaceService.OnNotifyStatoOrdine(IdOrdine);
      _UserInterfaceService.OnNotifyStatoProdotti(Ordine.IdCassa);
    }
#endregion
  }
}




