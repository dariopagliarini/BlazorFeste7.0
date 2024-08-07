﻿using BlazorFeste.Data.Models;
using BlazorFeste.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Text.Json;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Serilog;
using Newtonsoft.Json.Linq;
using BlazorFeste.DataAccess;
using Blazored.Toast.Services;
using System.Net;
using System.Text;

namespace BlazorFeste.Components
{
  public class DataChange
  {
    [JsonProperty("key")]
    public int Key { get; set; }

    [JsonProperty("type")]
    public string Type { get; set; }

    [JsonProperty("data")]
    public object Data { get; set; }
  }
  public partial class GestioneAnagrProdotti : IDisposable
  {
    #region Inject
    [Inject] public UserInterfaceService _UserInterfaceService { get; init; }
    [Inject] public FesteDataAccess festeDataAccess { get; init; }
    [Inject] public IJSRuntime JSRuntime { get; init; }
    #endregion

    #region Variabili
    private IJSObjectReference Module;
    private Task<IJSObjectReference> _jsModule;
    private Task<IJSObjectReference> JsModule => _jsModule ??= JSRuntime.InvokeAsync<IJSObjectReference>("import", "./js/GestioneAnagrProdottiObj.js").AsTask();

    private DotNetObjectReference<GestioneAnagrProdotti> objRef;

    //List<StatoCasse> _qryStatoCasse = new List<StatoCasse>();
    //List<StatoOrdini> _qryStatoOrdini = new List<StatoOrdini>();
    //List<AnagrProdotti > _qryStatoProdotti = new List<AnagrProdotti>();

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

        Module = (await JsModule);
        
        await Module.InvokeVoidAsync("GestioneAnagrProdottiObj.init", objRef, _UserInterfaceService.AnagrProdotti.Values);
      }
      else
      {
        if (Module != null)
        {
        }
        else
        {
          Module = (await JsModule);

          await Module.InvokeVoidAsync("GestioneAnagrProdottiObj.init", objRef, _UserInterfaceService.AnagrProdotti.Values);
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
    public async void BatchUpdateRequest(List<DataChange> changes)
    {
      foreach (var change in changes)
      {
        AnagrProdotti prodotto;

        if (change.Type == "update")
        {
          prodotto = _UserInterfaceService.AnagrProdotti.First(p => p.Key == change.Key).Value;

          JsonConvert.PopulateObject(change.Data.ToString(), prodotto);

          if (String.IsNullOrEmpty(prodotto.BackColor))
          {
            prodotto.BackColor = _UserInterfaceService.AnagrListe.Where(w => w.IdLista == prodotto.IdLista).FirstOrDefault().BackColor;
          }
          if (String.IsNullOrEmpty(prodotto.ForeColor))
          {
            prodotto.ForeColor = _UserInterfaceService.AnagrListe.Where(w => w.IdLista == prodotto.IdLista).FirstOrDefault().ForeColor;
          }
          await festeDataAccess.UpdateAnagrProdottiAsync(prodotto);
        }
        _UserInterfaceService.OnNotifyAnagrProdotti(false);
      }
    }
    #endregion  
  }
}
