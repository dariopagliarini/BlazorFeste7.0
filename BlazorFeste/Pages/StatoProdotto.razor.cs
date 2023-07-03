using BlazorFeste.Data.Models;
using BlazorFeste.DataAccess;
using BlazorFeste.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Serilog;

using System.Reflection;

namespace BlazorFeste.Pages
{
  public partial class StatoProdotto : IDisposable
  {
    [Parameter]
    public int IdProdotto { get; set; }

    #region Inject
    [Inject] public UserInterfaceService _UserInterfaceService { get; init; }
    #endregion

    #region Variabili

    AnagrProdotti Prodotto { get; set; } = new AnagrProdotti();
    #endregion

    #region LifeCycle
    protected override async Task OnInitializedAsync()
    {
      _UserInterfaceService.NotifyStatoOrdine += OnNotifyStatoOrdine;
      _UserInterfaceService.NotifyNuovoOrdine += OnNotifyNuovoOrdine;

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
      Prodotto = (_UserInterfaceService.AnagrProdotti.Values.Where(w => w.IdProdotto == IdProdotto)).FirstOrDefault();

      await base.OnParametersSetAsync();
    }
    public void Dispose()
    {
      _UserInterfaceService.NotifyStatoOrdine -= OnNotifyStatoOrdine;
      _UserInterfaceService.NotifyNuovoOrdine -= OnNotifyNuovoOrdine;
    }
    #endregion

    #region Metodi
    async void OnNotifyNuovoOrdine(object sender, DatiOrdine datiOrdine)
    {
      try
      {
        if (datiOrdine.ordineRighe.Where(w => w.IdProdotto == IdProdotto).Count() > 0)
        {
          // L'ordine ha qualcosa relativo al prodotto visualizzato
          //Log.Information($"Stato Prodotto - NotifyNuovoOrdine - Mi interessa!");
          Prodotto = (_UserInterfaceService.AnagrProdotti.Values.Where(w => w.IdProdotto == IdProdotto)).FirstOrDefault();

          await InvokeAsync(StateHasChanged);
        }
      }
      catch (TaskCanceledException tEx) { _ = tEx; }
      catch (Exception ex)
      {
        Log.Error(ex, "Code Exception");
      }
    }

    async void OnNotifyStatoOrdine(object sender, long idOrdine)
    {
      try
      {
        var DatiOrdine = from o in _UserInterfaceService.QryOrdini.Where(w => w.Key == idOrdine)
                         join r in _UserInterfaceService.QryOrdiniRighe on o.Key equals r.Key.Item1
                         join p in _UserInterfaceService.AnagrProdotti.Values.Where(w => w.IdProdotto == Prodotto.IdProdotto) on r.Value.IdProdotto equals p.IdProdotto
                         select new { o, r, p };

        if (DatiOrdine.Any())
        {
          // L'ordine ha qualcosa relativo al prodotto visualizzato
          //Log.Information($"Stato Prodotto - NotifyStatoOrdine - Mi interessa!");
          Prodotto = DatiOrdine.Select(w => w.p).FirstOrDefault();

          await InvokeAsync(StateHasChanged);
        }
      }
      catch (TaskCanceledException tEx) { _ = tEx; }
      catch (Exception ex)
      {
        Log.Error(ex, "Code Exception");
      }

    }
    #endregion

  }
}
