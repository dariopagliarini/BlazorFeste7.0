﻿using BlazorFeste.DataAccess;
using BlazorFeste.Services;
using BlazorFeste.Data.Models;
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
    [Inject] public ClientInformationService clientInfo { get; init; }
    [Inject] public NavigationManager _NavigationManager { get; init; }
    [Inject] public UserInterfaceService _UserInterfaceService { get; init; }
    [Inject] public FesteDataAccess _FesteDataAccess { get; init; }

    [Inject] public IJSRuntime JSRuntime { get; init; }
    #endregion

    #region Variabili
    private IJSObjectReference Module;
    private Task<IJSObjectReference> _jsModule;
    private Task<IJSObjectReference> JsModule => _jsModule ??= JSRuntime.InvokeAsync<IJSObjectReference>("import", "./js/DashBoardObj.js").AsTask();

    private DotNetObjectReference<Dashboard> objRef;

    List<StatoCasse> _qryStatoCasse = new();
    List<StatoOrdini> _qryStatoOrdini = new();
    List<StatoListe> _qryStatoListe = new();

//    List<ArchOrdini> _qryOrdini = new();
//    List<ArchOrdiniRighe> _qryOrdiniRighe = new();
//    List<AnagrCasse> _anagrCasse = new();
//    List<AnagrListe> _anagrListe = new();
//    List<AnagrProdotti> _anagrProdotti = new();

    int memoMinuto = -1;

    DateTime memoLastUtcNow_qryStatoCasse = default;
    DateTime memoLastUtcNow_qryStatoOrdini = default;
    DateTime memoLastUtcNow_qryStatoListe = default;

    string JSON_qryStatoCasse = string.Empty;
    string JSON_qryStatoOrdini = string.Empty;
    string JSON_qryStatoListe = string.Empty;

    string strElapsed = string.Empty;

    private CamelCasePropertyNamesContractResolver contractResolver = new CamelCasePropertyNamesContractResolver { NamingStrategy = new CamelCaseNamingStrategy { } };
    #endregion

    #region LifeCycle
    protected override Task OnInitializedAsync()
    {
      _UserInterfaceService.NotifyDashboard += OnNotifyDashboard;

      return base.OnInitializedAsync();
    }
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
      if (firstRender)
      {
        objRef = DotNetObjectReference.Create(this);

        Module = (await JsModule);

        await Module.InvokeVoidAsync("DashBoardObj.init", objRef, _UserInterfaceService.AnagrCasse, _UserInterfaceService.AnagrProdotti.Values);
      }
      await base.OnAfterRenderAsync(firstRender);
    }
    public void Dispose()
    {
      _UserInterfaceService.NotifyDashboard -= OnNotifyDashboard;

      //_jsModule?.Result.InvokeVoidAsync("DashBoardObj.dispose");
      objRef?.Dispose();
    }
    #endregion

    #region Eventi
    #endregion

    #region Metodi
    void ElaboraDashBoard(DatiNotifyDashboard _datiFesta)
    {
      try
      {
        var DatiFestaInCorso = from o in _datiFesta.Ordini
                               join r in _datiFesta.OrdiniRighe on o.IdOrdine equals r.IdOrdine
                               join c in _datiFesta.AnagrCasse on o.IdCassa equals c.IdCassa
                               select new
                               {
                                 o, r, c
                               };

        if (DatiFestaInCorso.Any())
        {
          var loc_qryStatoCasse = (from r in DatiFestaInCorso.Where(w => w.c.Visibile.Value)
                                   group r by new { r.o.IdCassa, r.r.IdProdotto } into g
                                   orderby g.Key.IdCassa
                                   select new
                                   {
                                     IdCassa = g.Key.IdCassa,
                                     IdProdotto = g.Key.IdProdotto,
                                     Importo = g.Sum(s => s.r.Importo),
                                     QuantitàProdotto = g.Sum(s => s.r.QuantitàProdotto),
                                   });

          var loc_qryStatoPOS = (from r in DatiFestaInCorso.Where(w => w.c.Visibile.Value)
                                   group r by new { r.o.IdCassa, r.o.PagamentoConPOS } into g
                                   orderby g.Key.IdCassa
                                   select new
                                   {
                                     IdCassa = g.Key.IdCassa,
                                     PagamentoConPOS = g.Key.PagamentoConPOS,
                                     Importo = g.Sum(s => s.r.Importo),
                                     QuantitàProdotto = g.Sum(s => s.r.QuantitàProdotto),
                                   });

          var loc_qryStatoProdotti = (from r in DatiFestaInCorso.Where(w => w.c.Visibile.Value)
                                      group r by r.r.IdProdotto into g
                                      orderby g.Key
                                      select new
                                      {
                                        IdProdotto = g.Key,
                                        Importo = g.Sum(s => s.r.Importo),
                                        QuantitàProdotto = g.Sum(s => s.r.QuantitàProdotto),
                                      });

          _qryStatoCasse = (from c in loc_qryStatoCasse
                            group c by c.IdCassa into g
                            orderby g.Key
                            select new StatoCasse
                            {
                              IdCassa = g.Key,
                              Importo = g.Sum(s => s.Importo),
                              QuantitàProdotto = g.Sum(s => s.QuantitàProdotto),
                              ImportoContanti = (from c in loc_qryStatoPOS
                                                 where c.PagamentoConPOS == false && c.IdCassa == g.Key
                                                 select c.Importo
                                                 ).FirstOrDefault(),
                              ImportoPOS = (from c in loc_qryStatoPOS
                                            where c.PagamentoConPOS == true && c.IdCassa == g.Key
                                            select c.Importo
                                                 ).FirstOrDefault()
                            }).ToList();

          _qryStatoOrdini = (from p in _datiFesta.AnagrProdotti
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

          _qryStatoListe = (from r in DatiFestaInCorso
                            join p in _datiFesta.AnagrProdotti on r.r.IdProdotto equals p.IdProdotto
                            join c in _datiFesta.AnagrListe.Where(w => w.Visibile.Value) on p.IdLista equals c.IdLista
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
        }
        else
        {
          _qryStatoCasse = (from c in _datiFesta.AnagrCasse.Where(w => w.Visibile.Value)
                            orderby c.IdCassa
                            select new StatoCasse
                            {
                              IdCassa = c.IdCassa,
                              Importo = 0.000001,
                              QuantitàProdotto = 0,
                              ImportoContanti = 0,
                              ImportoPOS = 0
                            }
          ).ToList();

          _qryStatoOrdini = (from p in _datiFesta.AnagrProdotti
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

          _qryStatoListe = (from c in _datiFesta.AnagrListe.Where(w => w.Visibile.Value)
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
      catch (TaskCanceledException tEx) { _ = tEx; }
      catch (Exception ex)
      {
        Log.Error(ex, $"{clientInfo.IPAddress} - ElaboraDashBoard");
      }
    }
       
    async void OnNotifyDashboard(object sender, DatiNotifyDashboard _datiDashboard)
    {
      try
      {
        ElaboraDashBoard(_datiDashboard);

        if (Module != null)
        {
          Dictionary<string, object> sendDict = new Dictionary<string, object>();

          if (DateTime.UtcNow.Minute != memoMinuto)
          {
            memoMinuto = DateTime.UtcNow.Minute;

            var diffOfDates = DateTime.UtcNow - memoLastUtcNow_qryStatoCasse;
            if (diffOfDates.TotalSeconds > 30)
            {
              sendDict.Add("qryStatoCasse", _qryStatoCasse);
              JSON_qryStatoCasse = JsonConvert.SerializeObject(_qryStatoCasse);
              memoLastUtcNow_qryStatoCasse = DateTime.UtcNow;
            }

            diffOfDates = DateTime.UtcNow - memoLastUtcNow_qryStatoOrdini;
            if (diffOfDates.TotalSeconds > 30)
            {
              sendDict.Add("qryStatoOrdini", _qryStatoOrdini);
              JSON_qryStatoOrdini = JsonConvert.SerializeObject(_qryStatoOrdini);
              memoLastUtcNow_qryStatoOrdini = DateTime.UtcNow;
            }

            diffOfDates = DateTime.UtcNow - memoLastUtcNow_qryStatoListe;
            if (diffOfDates.TotalSeconds > 30)
            {
              sendDict.Add("qryStatoListe", _qryStatoListe);
              JSON_qryStatoListe = JsonConvert.SerializeObject(_qryStatoListe);
              memoLastUtcNow_qryStatoListe = DateTime.UtcNow;
            }
          }
          else
          {
            if (!JToken.DeepEquals(JSON_qryStatoCasse, JsonConvert.SerializeObject(_qryStatoCasse)))
            {
              sendDict.Add("qryStatoCasse", _qryStatoCasse);
              JSON_qryStatoCasse = JsonConvert.SerializeObject(_qryStatoCasse);
              memoLastUtcNow_qryStatoCasse = DateTime.UtcNow;
            }
            if (!JToken.DeepEquals(JSON_qryStatoOrdini, JsonConvert.SerializeObject(_qryStatoOrdini)))
            {
              sendDict.Add("qryStatoOrdini", _qryStatoOrdini);
              JSON_qryStatoOrdini = JsonConvert.SerializeObject(_qryStatoOrdini);
              memoLastUtcNow_qryStatoOrdini = DateTime.UtcNow;
            }
            if (!JToken.DeepEquals(JSON_qryStatoListe, JsonConvert.SerializeObject(_qryStatoListe)))
            {
              sendDict.Add("qryStatoListe", _qryStatoListe);
              JSON_qryStatoListe = JsonConvert.SerializeObject(_qryStatoListe);
              memoLastUtcNow_qryStatoListe = DateTime.UtcNow;
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

          await Module.InvokeVoidAsync("DashBoardObj.init", objRef, _UserInterfaceService.AnagrCasse, _UserInterfaceService.AnagrProdotti.Values);
        }
        strElapsed = $"{DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss.fff")} - {_datiDashboard.elapsed_GetDatabaseData}/{_datiDashboard.elapsed_GetDashBoardData} msec";

        await InvokeAsync(StateHasChanged);
      }
      catch (TaskCanceledException tEx) { _ = tEx; }
      catch (Exception ex)
      {
        Log.Error(ex, $"{clientInfo.IPAddress} - OnNotifyDashboard");
      }
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
