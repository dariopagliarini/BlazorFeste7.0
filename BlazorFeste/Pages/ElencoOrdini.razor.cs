using BlazorFeste.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace BlazorFeste.Pages
{
  public partial class ElencoOrdini : IDisposable
  {
    private class Ordine_Righe
    {
      public int IdRiga { get; set; }
      public string NomeProdotto { get; set; }
      public int QuantitàProdotto { get; set; }
      public double Importo { get; set; }
      public int IdStatoRiga { get; set; }
    }
    private class Ordine
    {
      public long IdOrdine { get; set; }
      public string Cassa { get; set; }
      public string DataOra { get; set; }
      public string TipoOrdine { get; set; }
      public string Tavolo { get; set; }
      public string NumeroCoperti { get; set; }
      public string Referente { get; set; }
      public int IdStatoOrdine { get; set; }
      public string Timestamp { get; set; }
      public DateTime DataAssegnazione { get; set; }
      public List<Ordine_Righe> Righe { get; set; }
    }

    #region Inject
    [Inject] public UserInterfaceService _UserInterfaceService { get; init; }
    [Inject] public IJSRuntime JSRuntime { get; init; }
    #endregion

    #region Variabili
    private IJSObjectReference Module;
    private Task<IJSObjectReference> _jsModule;
    private Task<IJSObjectReference> JsModule => _jsModule ??= JSRuntime.InvokeAsync<IJSObjectReference>("import", "./js/ElencoOrdiniObj.js").AsTask();

    private DotNetObjectReference<ElencoOrdini> objRef;

    string strElapsedMsec = string.Empty;
    #endregion

    #region LifeCycle
    protected override Task OnInitializedAsync()
    {
      //      _UserInterfaceService.UpdateListe += OnUpdateListe;

      return base.OnInitializedAsync();
    }
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
      if (firstRender)
      {
        objRef = DotNetObjectReference.Create(this);

        Module = (await JsModule);

#if THREADSAFE
        var Ordini = from o in _UserInterfaceService.QryOrdini.Select(s => s.Value).OrderByDescending(k => k.Timestamp)
                     select new Ordine
                     {
                       IdOrdine = o.IdOrdine,
                       DataOra = o.DataOra.ToString("HH:mm:ss"),
                       Cassa = o.Cassa,
                       Timestamp = o.Timestamp.ToString("HH:mm:ss"),
                       TipoOrdine = o.TipoOrdine,
                       Tavolo = o.Tavolo,
                       NumeroCoperti = o.NumeroCoperti,
                       Referente = o.Referente,
                       IdStatoOrdine = o.IdStatoOrdine,
                       Righe = (from r in _UserInterfaceService.QryOrdiniRighe.Where(w => w.Key.Item1 == o.IdOrdine)
                                join p in _UserInterfaceService.AnagrProdotti
                                on r.Value.IdProdotto equals p.Key
                                orderby r.Value.IdProdotto
                                select new Ordine_Righe
                                {
                                  IdRiga = r.Value.IdRiga,
                                  NomeProdotto = p.Value.NomeProdotto,
                                  QuantitàProdotto = r.Value.QuantitàProdotto,
                                  Importo = r.Value.Importo,
                                  IdStatoRiga = r.Value.IdStatoRiga
                                }).ToList()
                     };
#else
        var Ordini = from o in _UserInterfaceService.QryOrdini.OrderByDescending(k => k.Timestamp)
                     select new Ordine
                     {
                       IdOrdine = o.IdOrdine,
                       DataOra = o.DataOra.ToString("HH:mm:ss"),
                       Cassa = o.Cassa,
                       Timestamp = o.Timestamp.ToString("HH:mm:ss"),
                       TipoOrdine = o.TipoOrdine,
                       Tavolo = o.Tavolo,
                       NumeroCoperti = o.NumeroCoperti,
                       Referente = o.Referente,
                       IdStatoOrdine = o.IdStatoOrdine,
                       Righe = (from r in _UserInterfaceService.QryOrdiniRighe.Where(w => w.IdOrdine == o.IdOrdine)
                                join p in _UserInterfaceService.AnagrProdotti
                                  on r.IdProdotto equals p.IdProdotto
                                orderby r.IdProdotto
                                select new Ordine_Righe
                                {
                                  IdRiga = r.IdRiga,
                                  NomeProdotto = p.NomeProdotto,
                                  QuantitàProdotto = r.QuantitàProdotto,
                                  Importo = r.Importo,
                                  IdStatoRiga = r.IdStatoRiga
                                }).ToList()
                     };
#endif
        await Module.InvokeVoidAsync("ElencoOrdiniObj.renderGridOrdini", objRef, "#myGridOrdini", Ordini);
        await Module.InvokeVoidAsync("ElencoOrdiniObj.renderGridRighe", "#myGridRighe");
      }
      await base.OnAfterRenderAsync(firstRender);
    }
    public void Dispose()
    {
      //_UserInterfaceService.UpdateListe -= OnUpdateListe;
      objRef?.Dispose();
    }
    #endregion
  }
}
