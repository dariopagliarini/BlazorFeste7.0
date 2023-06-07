using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace BlazorFeste.Components
{
  public partial class MeasureLatency 
  {
    [Inject] IJSRuntime JSRuntime { get; init; }

    private DateTime startTime;
    private TimeSpan? latency;

    #region LifeCycle
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
      if (firstRender)
      {
        startTime = DateTime.UtcNow;
        var _ = await JSRuntime.InvokeAsync<string>("toString");
        latency = DateTime.UtcNow - startTime;
        StateHasChanged();
      }
      await base.OnAfterRenderAsync(firstRender);
    }
    #endregion  
  }
}
