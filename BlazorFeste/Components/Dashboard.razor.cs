using BlazorFeste.Data.Models;
using BlazorFeste.Services;
using BlazorFeste.Util;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Serilog;

namespace BlazorFeste.Components
{
  public partial class Dashboard : IDisposable
  {
    #region Inject
    [Inject] public NavigationManager _NavigationManager { get; init; }
    [Inject] public UserInterfaceService _UserInterfaceService { get; init; }
    [Inject] public IJSRuntime JSRuntime { get; init; }
    #endregion

    #region Variabili
    private IJSObjectReference Module;
    private Task<IJSObjectReference> _jsModule;
    private Task<IJSObjectReference> JsModule => _jsModule ??= JSRuntime.InvokeAsync<IJSObjectReference>("import", "./js/DashBoardObj.js").AsTask();

    private DotNetObjectReference<Dashboard> objRef;

    List<StatoCasse> _qryStatoCasse = new List<StatoCasse>();
    List<StatoOrdini> _qryStatoOrdini = new List<StatoOrdini>();
    List<StatoListe> _qryStatoListe = new List<StatoListe>();

    int memoMinuto = -1;

    string JSON_qryStatoCasse  = string.Empty;
    string JSON_qryStatoOrdini = string.Empty;
    string JSON_qryStatoListe  = string.Empty;

    string strElapsed = string.Empty;

    private CamelCasePropertyNamesContractResolver contractResolver = new CamelCasePropertyNamesContractResolver { NamingStrategy = new CamelCaseNamingStrategy { } };
    #endregion

    #region LifeCycle
    protected override Task OnInitializedAsync()
    {
      _UserInterfaceService.UpdateListe += OnUpdateListe;

      return base.OnInitializedAsync();
    }
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
      if (firstRender)
      {
        objRef = DotNetObjectReference.Create(this);

        Module = (await JsModule);

#if THREADSAFE
        await Module.InvokeVoidAsync("DashBoardObj.init", objRef, _UserInterfaceService.AnagrCasse, _UserInterfaceService.AnagrProdotti.Values);
#else
        await Module.InvokeVoidAsync("DashBoardObj.init", objRef, _UserInterfaceService.AnagrCasse, _UserInterfaceService.AnagrProdotti);
#endif
      }
      else
      {
        if (Module != null)
        {
          Dictionary<string, object> sendDict = new Dictionary<string, object>();

          ElaboraDashBoard();

          if (DateTime.UtcNow.Minute != memoMinuto)
          {
            sendDict.Add("qryStatoCasse", _qryStatoCasse);
            sendDict.Add("qryStatoOrdini", _qryStatoOrdini);
            sendDict.Add("qryStatoListe", _qryStatoListe);

            memoMinuto = DateTime.UtcNow.Minute;
          }
          else
          {
            if (!JToken.DeepEquals(JSON_qryStatoCasse, System.Text.Json.JsonSerializer.Serialize(_qryStatoCasse)))
            {
              sendDict.Add("qryStatoCasse", _qryStatoCasse);
              JSON_qryStatoCasse = System.Text.Json.JsonSerializer.Serialize(_qryStatoCasse);
            }
            if (!JToken.DeepEquals(JSON_qryStatoOrdini, System.Text.Json.JsonSerializer.Serialize(_qryStatoOrdini)))
            {
              sendDict.Add("qryStatoOrdini", _qryStatoOrdini);
              JSON_qryStatoOrdini = System.Text.Json.JsonSerializer.Serialize(_qryStatoOrdini);
            }
            if (!JToken.DeepEquals(JSON_qryStatoListe, System.Text.Json.JsonSerializer.Serialize(_qryStatoListe)))
            {
              sendDict.Add("qryStatoListe", _qryStatoListe);
              JSON_qryStatoListe = System.Text.Json.JsonSerializer.Serialize(_qryStatoListe);
            }
          }

          if (sendDict.Count > 0)
          {
            await Module.InvokeVoidAsync("DashBoardObj.update",
              JsonConvert.SerializeObject(sendDict, new JsonSerializerSettings { ContractResolver = contractResolver }));
          }
        }
        else
        {
          Module = (await JsModule);

#if THREADSAFE
          await Module.InvokeVoidAsync("DashBoardObj.init", objRef, _UserInterfaceService.AnagrCasse, _UserInterfaceService.AnagrProdotti.Values);
#else
          await Module.InvokeVoidAsync("DashBoardObj.init", objRef, _UserInterfaceService.AnagrCasse, _UserInterfaceService.AnagrProdotti);
#endif
        }
      }
      await base.OnAfterRenderAsync(firstRender);
    }
    public void Dispose()
    {
      _UserInterfaceService.UpdateListe -= OnUpdateListe;

      //_jsModule?.Result.InvokeVoidAsync("DashBoardObj.dispose");
      objRef?.Dispose();
    }
    #endregion

    #region Eventi
    #endregion

    #region Metodi
    void ElaboraDashBoard()
    {
      try
      {
#if THREADSAFE
        var DatiFestaInCorso = from o in _UserInterfaceService.QryOrdini.Where(w => w.Value.DataAssegnazione == _UserInterfaceService.DtFestaInCorso)
                               join r in _UserInterfaceService.QryOrdiniRighe on o.Key equals r.Key.Item1
                               select new { o, r,
                                 Cassa = _UserInterfaceService.AnagrCasse.Where(w => w.IdCassa == o.Value.IdCassa).FirstOrDefault()
                               };
#else
        var DatiFestaInCorso = from o in _UserInterfaceService.QryOrdini.Where(w => w.DataAssegnazione == _UserInterfaceService.DtFestaInCorso)
                               join r in _UserInterfaceService.QryOrdiniRighe on o.IdOrdine equals r.IdOrdine

                               select new { o, r };
#endif
        if (DatiFestaInCorso.Any())
        {
#if THREADSAFE
          var loc_qryStatoCasse = (from r in DatiFestaInCorso.Where(w => w.Cassa.Visibile.Value)
                                   group r by new { r.o.Value.IdCassa, r.r.Value.IdProdotto } into g
                                   orderby g.Key.IdCassa
                                   select new
                                   {
                                     IdCassa = g.Key.IdCassa,
                                     IdProdotto = g.Key.IdProdotto,
                                     Importo = g.Sum(s => s.r.Value.Importo),
                                     QuantitàProdotto = g.Sum(s => s.r.Value.QuantitàProdotto),
                                   });

          var loc_qryStatoProdotti = (from r in DatiFestaInCorso.Where(w => w.Cassa.Visibile.Value)
                                      group r by r.r.Value.IdProdotto into g
                                      orderby g.Key
                                      select new
                                      {
                                        IdProdotto = g.Key,
                                        Importo = g.Sum(s => s.r.Value.Importo),
                                        QuantitàProdotto = g.Sum(s => s.r.Value.QuantitàProdotto),
                                      });
#else
          var loc_qryStatoCasse = (from r in DatiFestaInCorso.Where(w => w.Cassa.Visibile.Value)
                                   group r by new { r.o.IdCassa, r.r.IdProdotto } into g
                                   orderby g.Key.IdCassa
                                   select new
                                   {
                                     IdCassa = g.Key.IdCassa,
                                     IdProdotto = g.Key.IdProdotto,
                                     Importo = g.Sum(s => s.r.Importo),
                                     QuantitàProdotto = g.Sum(s => s.r.QuantitàProdotto),
                                   });
          var loc_qryStatoProdotti = (from r in DatiFestaInCorso.Where(w => w.Cassa.Visibile.Value)
                                      group r by r.r.IdProdotto into g
                                      orderby g.Key
                                      select new
                                      {
                                        IdProdotto = g.Key,
                                        Importo = g.Sum(s => s.r.Importo),
                                        QuantitàProdotto = g.Sum(s => s.r.QuantitàProdotto),
                                      });
#endif
          // _UserInterfaceService.qryStatoCasse.OrderBy(o => o.IdCassa).ToList();
          _qryStatoCasse = (from c in loc_qryStatoCasse
                            group c by c.IdCassa into g
                            orderby g.Key
                            select new StatoCasse
                            {
                              IdCassa = g.Key,
                              Importo = g.Sum(s => s.Importo),
                              QuantitàProdotto = g.Sum(s => s.QuantitàProdotto),
                            }).ToList();
          //_qryStatoOrdini = _UserInterfaceService.qryStatoOrdini.OrderBy(o => o.IdProdotto).ToList();
#if THREADSAFE
          _qryStatoOrdini = (from p in _UserInterfaceService.AnagrProdotti.Values
#else
          _qryStatoOrdini = (from p in _UserInterfaceService.AnagrProdotti
#endif
                             join r in loc_qryStatoProdotti on p.IdProdotto equals r.IdProdotto
                             orderby p.IdProdotto
                             where r.QuantitàProdotto > 0
                             select new StatoOrdini
                             {
                               IdProdotto = p.IdProdotto,
                               NomeProdotto = p.NomeProdotto.CR_to_Space(),
                               Importo = r.Importo,
                               Quantità = r.QuantitàProdotto,
                               statoCassa = (from c in loc_qryStatoCasse
                                             group c by c.IdCassa into g
                                             orderby g.Key
                                             select new StatoCasse
                                             {
                                               IdCassa = g.Key,
                                               Importo = g.Where(c => c.IdProdotto == p.IdProdotto).Sum(s => s.Importo),
                                               QuantitàProdotto = g.Where(c => c.IdProdotto == p.IdProdotto).Sum(s => s.QuantitàProdotto)
                                             }).ToList(),
                             }
            ).ToList();

          //_qryStatoListe = _UserInterfaceService.qryStatoListe.OrderBy(o => o.IdLista).ToList();
#if THREADSAFE
          _qryStatoListe = (from r in DatiFestaInCorso
                            join p in _UserInterfaceService.AnagrProdotti.Values on r.r.Value.IdProdotto equals p.IdProdotto
                            join c in _UserInterfaceService.AnagrListe.Where(w => w.Visibile.Value) on p.IdLista equals c.IdLista
                            group r by new { c.IdLista, c.Lista } into g
                            orderby g.Key.IdLista
                            select new StatoListe
                            {
                              IdLista = g.Key.IdLista,
                              Lista = g.Key.Lista,
                              OrdiniInCoda = g.Where(c => c.r.Value.IdStatoRiga <= 0).Sum(s => s.r.Value.QuantitàProdotto),
                              OrdiniInCorso = g.Where(c => c.r.Value.IdStatoRiga == 1 || c.r.Value.IdStatoRiga == 2).Sum(s => s.r.Value.QuantitàProdotto),
                              OrdiniEvasi = g.Where(c => c.r.Value.IdStatoRiga == 3).Sum(s => s.r.Value.QuantitàProdotto)
                            }
          ).ToList();
#else
          _qryStatoListe = (from r in DatiFestaInCorso
                            join p in _UserInterfaceService.AnagrProdotti on r.r.IdProdotto equals p.IdProdotto
                            join c in _UserInterfaceService.AnagrListe.Where(w => w.Visibile.Value) on p.IdLista equals c.IdLista
                            group r by new { c.IdLista, c.Lista } into g
                            orderby g.Key.IdLista
                            select new StatoListe
                            {
                              IdLista = g.Key.IdLista,
                              Lista = g.Key.Lista,
                              OrdiniInCoda = g.Where(c => c.r.IdStatoRiga <= 0).Sum(s => s.r.QuantitàProdotto),
                              OrdiniInCorso = g.Where(c => c.r.IdStatoRiga == 1 || c.r.IdStatoRiga == 2).Sum(s => s.r.QuantitàProdotto),
                              OrdiniEvasi = g.Where(c => c.r.IdStatoRiga == 3).Sum(s => s.r.QuantitàProdotto)
                            }
          ).ToList();
#endif
        }
        else
        {
          _qryStatoCasse = (from c in _UserInterfaceService.AnagrCasse.Where(w => w.Visibile.Value)
                            orderby c.IdCassa
                                                 select new StatoCasse
                                                 {
                                                   IdCassa = c.IdCassa,
                                                   Importo = 0.000001,
                                                   QuantitàProdotto = 0
                                                 }
          ).ToList();

#if THREADSAFE          
          _qryStatoOrdini = (from p in _UserInterfaceService.AnagrProdotti.Values
#else
          _qryStatoOrdini = (from p in _UserInterfaceService.AnagrProdotti

#endif
                             orderby p.IdProdotto
                             where p.Stato
                                                  select new StatoOrdini
                                                  {
                                                    IdProdotto = p.IdProdotto,
                                                    NomeProdotto = p.NomeProdotto.CR_to_Space(),
                                                    Importo = 0,
                                                    Quantità = 0,
                                                    statoCassa = (from c in _qryStatoCasse
                                                                  group c by c.IdCassa into g
                                                                  orderby g.Key
                                                                  select new StatoCasse
                                                                  {
                                                                    IdCassa = g.Key,
                                                                    Importo = 0,
                                                                    QuantitàProdotto = 0
                                                                  }
                                                                  ).ToList(),
                                                  }
          ).ToList();

          _qryStatoListe = (from c in _UserInterfaceService.AnagrListe.Where(w => w.Visibile.Value)
                                                 select new StatoListe
                                                 {
                                                   IdLista = c.IdLista,
                                                   Lista = c.Lista,
                                                   OrdiniInCoda = 0,
                                                   OrdiniInCorso = 0,
                                                   OrdiniEvasi = 0
                                                 }
          ).ToList();
        }
      }
      catch (Exception ex)
      {
        Log.Error($"ElaboraDashBoard - ExecuteAsync - {ex.Message}");
      }
    }
    async void OnUpdateListe(object sender, string ElapsedInfo)
    {
      strElapsed = $"{DateTime.Now.ToString()} - {ElapsedInfo} msec";
      await InvokeAsync(StateHasChanged);
    }
#endregion

#region JSInvokable
    [JSInvokable("OpenListaDetail")]
    public void OpenListaDetail(StatoListe _statoLista)
    {
      if (_statoLista.IdLista != 0)
        _NavigationManager.NavigateTo($"/GestioneLista/{_statoLista.IdLista}");
    }
#endregion
  }
}
