using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Timers;

namespace BlazorFeste.Components
{
  public partial class CountDownSpinner : IDisposable
  {
    [Parameter] public string Name { get; set; }
    [Parameter] public int Time { get; set; } = 10;
    [Parameter] public EventCallback<string> CountdownEnded { get; set; }
    [Parameter] public bool StartOnCreate { get; set; } = true;
    [Parameter] public bool ClientSide { get; init; } = true;
    [Parameter] public string Size { get; set; } = "100px";

    #region Inject
    [Inject] private IJSRuntime _js { get; init; }
    #endregion

    #region Variabili
    private System.Timers.Timer countdownTimer;
    private int counter = 0;
    private bool stopped = false;
    private string timerBorderClass = "timerBorder";
    private string counterDivId = "a" + Guid.NewGuid().ToString();
    private DotNetObjectReference<CountDownSpinner> objRef;
    private Task<IJSObjectReference> _jsModule;
    private Task<IJSObjectReference> JsModule => _jsModule ??= _js.InvokeAsync<IJSObjectReference>("import", "./js/CountDownSpinnerObj.js").AsTask();
    #endregion

    #region LifeCycle
    protected override async Task OnInitializedAsync()
    {
      await base.OnInitializedAsync();
    }
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
      if (firstRender)
      {
        objRef = DotNetObjectReference.Create(this);

        if (StartOnCreate && ClientSide)
        {
          var _js = (await JsModule);
          await _js.InvokeVoidAsync("CountDownSpinnerObj.startTimer", $"#{counterDivId}", objRef, Time, Name/*, TimeStopped*/);
        }

        if (!ClientSide)
        {
          StartTimer();
        }
      }
      await base.OnAfterRenderAsync(firstRender);
    }
    protected override async Task OnParametersSetAsync()
    {
      Name = String.IsNullOrEmpty(Name) ? counterDivId : Name;

      await base.OnParametersSetAsync();
    }
    public void Dispose()
    {
      countdownTimer?.Dispose();
      objRef?.Dispose();
    }
    #endregion

    #region Eventi
    #endregion

    #region Metodi
    public void StartTimer()
    {
      counter = Time;
      countdownTimer = new(1000);
      countdownTimer.Elapsed += CountDownTimer;
      countdownTimer.AutoReset = false;
      countdownTimer.Enabled = true;
    }
    public async Task FreezeTimer()
    {
      if (!stopped)
      {
        countdownTimer.Stop();
        timerBorderClass = "timerBorder timerAnimationPaused";
      }
      else
      {
        countdownTimer.Start();
        timerBorderClass = "timerBorder";
      }
      stopped = !stopped;
      await InvokeAsync(StateHasChanged);
    }
    public async void CountDownTimer(Object source, ElapsedEventArgs e)
    {
      if (!stopped)
      {
        if (counter > 1)
        {
          counter -= 1;
        }
        else
        {
          if (CountdownEnded.HasDelegate)
          {
            await InvokeAsync(() => CountdownEnded.InvokeAsync(Name));
          }
          counter = Time;
        }
        await InvokeAsync(StateHasChanged);
      }
      countdownTimer?.Start();
    }
    #endregion

    #region JSInvokable
    [JSInvokable("OnCountdownEnd")]
    public async Task OnCountdownEnd(string CounterName)
    {
      if (CountdownEnded.HasDelegate)
        await CountdownEnded.InvokeAsync(CounterName);
    }
    #endregion
  }
}




