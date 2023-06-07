  public partial class GestioneLista : IDisposable
  {
    #region Inject
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

      }
      await base.OnAfterRenderAsync(firstRender);
    }
    protected override async Task OnParametersSetAsync()
    {
      await base.OnParametersSetAsync();
    }
    public void Dispose()
    {
    }
    #endregion

    #region Variabili
    #endregion

    #region Eventi
    #endregion

    #region Metodi
    #endregion

    #region JSInvokable
    #endregion
  }