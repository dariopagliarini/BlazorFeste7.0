using BlazorFeste.Services;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace BlazorFeste.Components
{
  public partial class TopBar : IDisposable
  {
    #region Inject
    [Inject] public NavigationManager _NavigationManager { get; init; }
    [Inject] public UserInterfaceService _UserInterfaceService { get; init; }
    [Inject] public IJSRuntime JSRuntime { get; init; }
    [Inject] public IWebHostEnvironment _iWebHostEnvironment { get; init; }
    [Inject] public ClientInformationService _clientInfo { get; init; }
    #endregion

    #region Variabili
    private IJSObjectReference Module;
    private Task<IJSObjectReference> _jsModule;
    private Task<IJSObjectReference> JsModule => _jsModule ??= JSRuntime.InvokeAsync<IJSObjectReference>("import", "./js/TopBarObj.js").AsTask();

    private DotNetObjectReference<TopBar> objRef;
    #endregion

    #region Metodi
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

        await Module.InvokeVoidAsync("TopBarObj.init", objRef, _UserInterfaceService.AnagrListe.Where(w => w.Visibile.Value),
          _iWebHostEnvironment.IsDevelopment() || _UserInterfaceService.AnagrClients.Where(w => w.Livello > 0).Select(s => s.IndirizzoIP).Contains(_clientInfo.IPAddress),
          _clientInfo
          );
      }
      await base.OnAfterRenderAsync(firstRender);
    }
    public void Dispose()
    {
      objRef?.Dispose();
    }
    #endregion

    #region JSInvokable
    [JSInvokable("NavigateToPage")]
    public void NavigateToPage(string navigateToPage)
    {
      _NavigationManager.NavigateTo($"/{navigateToPage}");
    }
    #endregion
  }
}
