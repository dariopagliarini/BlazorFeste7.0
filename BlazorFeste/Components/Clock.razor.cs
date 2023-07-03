using BlazorFeste.Services;
using BlazorFeste.Util;
using Microsoft.AspNetCore.Components;

using Serilog;

namespace BlazorFeste.Components
{
  public partial class Clock : IDisposable
  {
    [Parameter] public bool showOra { get; set; } = true;
    [Parameter] public bool showData { get; init; } = true;

    #region Inject
    [Inject] public UserInterfaceService _UserInterfaceService { get; init; } 
    #endregion

    #region Variabili
    String strData = String.Empty;
    String strOra = String.Empty;
    #endregion

    #region LifeCycle
    protected override async Task OnInitializedAsync()
    {
      _UserInterfaceService.NotifyDataOraServer += OnNotifyDataOraServer;

      await base.OnInitializedAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
      if (firstRender)
      {
        //strIPAddress = contextAccessor.HttpContext.Connection?.RemoteIpAddress.ToString();
      }
      await base.OnAfterRenderAsync(firstRender);
    }

    public void Dispose()
    {
      _UserInterfaceService.NotifyDataOraServer -= OnNotifyDataOraServer;
    }

    #endregion

    #region Eventi
    async void OnNotifyDataOraServer(object sender, DateTime adesso)
    {
      try
      {
        strOra = adesso.ToString("HH:mm:ss");
        strData = adesso.ToString("dddd dd MMM yyyy").FirstCharToUpper();

        await InvokeAsync(StateHasChanged);
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
