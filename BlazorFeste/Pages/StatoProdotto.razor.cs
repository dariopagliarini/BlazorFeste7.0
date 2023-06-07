using BlazorFeste.Data.Models;
using BlazorFeste.DataAccess;
using BlazorFeste.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Serilog;

namespace BlazorFeste.Pages
{
  public partial class StatoProdotto : IDisposable
  {
    [Parameter]
    public int IdProdotto { get; set; }

    #region Inject
    [Inject] public UserInterfaceService _UserInterfaceService { get; init; }
    [Inject] public FesteDataAccess festeDataAccess { get; init; }
    [Inject] public IJSRuntime JSRuntime { get; init; }
    #endregion

    #region Variabili

    AnagrProdotti Prodotto { get; set; } = new AnagrProdotti();
    #endregion

    #region LifeCycle
    protected override async Task OnInitializedAsync()
    {
      _UserInterfaceService.NotifyStatoOrdine += OnNotifyStatoOrdine;

      await base.OnInitializedAsync();
    }
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
      if (firstRender)
      {
      }
      await base.OnAfterRenderAsync(firstRender);
    }
    protected override async Task OnParametersSetAsync()
    {
#if THREADSAFE
      Prodotto = (_UserInterfaceService.AnagrProdotti.Values.Where(w => w.IdProdotto == IdProdotto)).FirstOrDefault();
#else
      Prodotto = (_UserInterfaceService.AnagrProdotti.Where(w => w.IdProdotto == IdProdotto)).FirstOrDefault();
#endif
      await base.OnParametersSetAsync();
    }
    public void Dispose()
    {
      _UserInterfaceService.NotifyStatoOrdine -= OnNotifyStatoOrdine;
    }
#endregion

#region Metodi
    async void OnNotifyStatoOrdine(object sender, long idOrdine)
    {
#if THREADSAFE
      var DatiOrdine = from o in _UserInterfaceService.QryOrdini.Where(w => w.Key == idOrdine)
                       join r in _UserInterfaceService.QryOrdiniRighe on o.Key equals r.Key.Item1
                       join p in _UserInterfaceService.AnagrProdotti.Values.Where(w => w.IdProdotto == Prodotto.IdProdotto) on r.Value.IdProdotto equals p.IdProdotto
                       select new { o, r, p };
#else
      var DatiOrdine = from o in _UserInterfaceService.QryOrdini.Where(w => w.IdOrdine == idOrdine)
                       join r in _UserInterfaceService.QryOrdiniRighe on o.IdOrdine equals r.IdOrdine
                       join p in _UserInterfaceService.AnagrProdotti.Where(w => w.IdProdotto == Prodotto.IdProdotto) on r.IdProdotto equals p.IdProdotto
                       select new { o, r, p };
#endif

      if (DatiOrdine.Any())
      {
        // L'ordine ha qualcosa relativo alla lista visualizzata
        //Log.Information($"Stato Prodotto - NotifyStatoOrdine - Mi interessa!");
        Prodotto = DatiOrdine.Select(w => w.p).FirstOrDefault();

        await InvokeAsync(StateHasChanged);
      }
    }
#endregion

  }
}
