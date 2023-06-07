using BlazorFeste.Components;
using BlazorFeste.DataAccess;
using BlazorFeste.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Newtonsoft.Json.Linq;

namespace BlazorFeste.Pages
{
  public partial class Servizio : IDisposable
  {
    #region Inject
    [Inject] public NavigationManager _NavigationManager { get; init; }
    [Inject] public UserInterfaceService _UserInterfaceService { get; init; }
    [Inject] public IJSRuntime JSRuntime { get; init; }
    #endregion

    #region Variabili
    private IJSObjectReference Module;
    private Task<IJSObjectReference> _jsModule;
    private Task<IJSObjectReference> JsModule => _jsModule ??= JSRuntime.InvokeAsync<IJSObjectReference>("import", "./js/ServizioObj.js").AsTask();

    private DotNetObjectReference<Servizio> objRef;

    private bool tab1 = true;
    private bool tab2 = false;
    private bool tab3 = false;
    #endregion

    #region Metodi
    public void DisplayTab(int TabNumber)
    {
      switch (TabNumber)
      {
        case 1:
          this.tab1 = true;
          this.tab2 = false;
          this.tab3 = false;
          break;

        case 2:
          this.tab1 = false;
          this.tab2 = true;
          this.tab3 = false;
          break;

        case 3:
          this.tab1 = false;
          this.tab2 = false; 
          this.tab3 = true;
          break;

        default:
          break;
      }

    }
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
        Module = (await JsModule);

        await Module.InvokeVoidAsync("ServizioObj.init", objRef, _UserInterfaceService.AnagrCasse);
      }
      else
      {
        if (Module != null)
        {
        }
        else
        {
          Module = (await JsModule);
          await Module.InvokeVoidAsync("ServizioObj.init", objRef, _UserInterfaceService.AnagrCasse);
        }
      }
      await base.OnAfterRenderAsync(firstRender);
    }
    public void Dispose()
    {
      _jsModule?.Result.InvokeVoidAsync("ServizioObj.dispose");
      objRef?.Dispose();
    }
    #endregion

    #region JSInvokable
    [JSInvokable("NavigateToPage")]
    public void NavigateToPage(string navigateToPage)
    {
      _NavigationManager.NavigateTo($"/{navigateToPage}");
    }
    [JSInvokable("RefreshAnagrafiche")]
    public void RefreshAnagrafiche()
    {
      _UserInterfaceService.DtFestaInCorso = DateTime.MinValue;
    }
    #endregion
  }
}
