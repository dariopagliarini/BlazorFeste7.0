using BlazorFeste.Classes;
using BlazorFeste.Data.Models;
using BlazorFeste.DataAccess;
using BlazorFeste.Services;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace BlazorFeste.Components
{
  public partial class GestioneAnagrListe : IDisposable
  {
    #region Inject
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
    #endregion  

  }
}
