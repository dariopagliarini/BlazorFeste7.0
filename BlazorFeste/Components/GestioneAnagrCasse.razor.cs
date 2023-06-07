using BlazorFeste.Data.Models;
using BlazorFeste.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;
using Serilog;
using System.Threading.Channels;
using BlazorFeste.DataAccess;
using BlazorFeste.Classes;

namespace BlazorFeste.Components
{
  public partial class GestioneAnagrCasse : IDisposable
  {
    #region Inject
    [Inject] public UserInterfaceService _UserInterfaceService { get; init; }
    [Inject] public FesteDataAccess festeDataAccess { get; init; }
    [Inject] public IJSRuntime JSRuntime { get; init; }
    #endregion

    #region Variabili
    private IJSObjectReference Module;
    private Task<IJSObjectReference> _jsModule;
    private Task<IJSObjectReference> JsModule => _jsModule ??= JSRuntime.InvokeAsync<IJSObjectReference>("import", "./js/GestioneAnagrCasseObj.js").AsTask();

    private DotNetObjectReference<GestioneAnagrCasse> objRef;
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

        var AnagrCasse = (await festeDataAccess.GetGenericQuery<AnagrCasse>("SELECT * FROM anagr_casse WHERE IdListino = @IdListino ORDER BY IdCassa ",
          new { IdListino = _UserInterfaceService.ArchFesta.IdListino })).ToList();

        Module = (await JsModule);
        await Module.InvokeVoidAsync("GestioneAnagrCasseObj.init", objRef, AnagrCasse);
      }
      else
      {
        if (Module != null)
        {
        }
        else
        {
          var AnagrCasse = (await festeDataAccess.GetGenericQuery<AnagrCasse>("SELECT * FROM anagr_casse WHERE IdListino = @IdListino ORDER BY IdCassa ",
            new { IdListino = _UserInterfaceService.ArchFesta.IdListino })).ToList();

          Module = (await JsModule);
          await Module.InvokeVoidAsync("GestioneAnagrCasseObj.init", objRef, AnagrCasse);
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
    public async Task<List<AnagrCasse>> BatchUpdateRequest(List<AnagrDataChange> changes)
    {
      foreach (var change in changes)
      {
        if (change.Type == "update")
        {
          await festeDataAccess.UpdateAnagrCasseAsync(change);
        }

        if (change.Type == "insert")
        {
          await festeDataAccess.InsertAnagrCasseAsync(change);
        }
      }
      var NewAnagrCasse = (await festeDataAccess.GetGenericQuery<AnagrCasse>("SELECT * FROM anagr_casse WHERE IdListino = @IdListino ORDER BY IdCassa ",
        new { IdListino = _UserInterfaceService.ArchFesta.IdListino })).ToList();

      // Aggiorna la variabile globale
      _UserInterfaceService.AnagrCasse = (await festeDataAccess.GetGenericQuery<AnagrCasse>("SELECT * FROM anagr_casse WHERE Abilitata <> 0 AND IdListino = @IdListino ORDER BY IdCassa ",
        new { IdListino = _UserInterfaceService.ArchFesta.IdListino })).ToList();

      _UserInterfaceService.OnNotifyAnagrCasse(false);

      return (NewAnagrCasse);
    }
    #endregion  

  }
}
