using Blazored.Toast.Services;

using BlazorFeste.Data.Models;
using BlazorFeste.DataAccess;
using BlazorFeste.Util;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;

using Serilog;

namespace BlazorFeste.Pages
{
  public partial class Rapporti : IDisposable
  {
    #region Inject
    [Inject] public IHttpContextAccessor httpContextAccessor { get; init; }
    [Inject] public IToastService toastService { get; init; }
    [Inject] public FesteDataAccess festeDataAccess { get; init; }
    [Inject] public IJSRuntime JSRuntime { get; init; }
    #endregion

    #region Variabili
    private int local_selFesta = -1;
    private string local_strAllSelected = string.Empty;

    private bool EsportaDisabled = false;

    private IJSObjectReference Module;
    private Task<IJSObjectReference> _jsModule;
    private Task<IJSObjectReference> JsModule => _jsModule ??= JSRuntime.InvokeAsync<IJSObjectReference>("import", "./js/RapportiObj.js").AsTask();

    CancellationTokenSource cts = new();
    CancellationToken ct { get => cts.Token; }

    private DotNetObjectReference<Rapporti> objRef;

    private List<ArchFeste> local_archFeste = new List<ArchFeste>();
    private List<ArchOrdini> local_qryOrdini = new List<ArchOrdini>();
    private List<ArchOrdiniRighe> local_qryOrdiniRighe = new List<ArchOrdiniRighe>();

    private List<AnagrProdotti> prodottiUsatiNellaFesta = new List<AnagrProdotti>();
    private List<AnagrCasse> local_Casse = new List<AnagrCasse>();

    private CamelCasePropertyNamesContractResolver contractResolver = new CamelCasePropertyNamesContractResolver { NamingStrategy = new CamelCaseNamingStrategy { } };
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

        Module = (await JsModule);

        local_archFeste = (await festeDataAccess.GetGenericQuery<ArchFeste>("SELECT * FROM arch_feste WHERE DataInizio <= NOW() AND Visibile = 1 ORDER BY DataInizio")).ToList();
        local_qryOrdini = (await festeDataAccess.GetGenericQuery<ArchOrdini>("SELECT * FROM arch_ordini WHERE DataAssegnazione <= @DataFine",
          new { DataFine = local_archFeste.Max(m => m.DataFine).Value })).ToList();

        var Feste = from f in local_archFeste
                    select new
                    {
                      IdFesta = f.IdFesta,
                      Festa = f.Festa,
                      Associazione = f.Associazione,
                      DataInizio = f.DataInizio.Value.ToString("dd/MM/yyyy"),
                      DataFine = f.DataFine.Value.ToString("dd/MM/yyyy"),
                      Giornate = from o in local_qryOrdini.Where(w => (w.DataAssegnazione >= f.DataInizio & w.DataAssegnazione <= f.DataFine))
                                 group o by o.DataAssegnazione into g
                                 select new
                                 {
                                   IdFesta = f.IdFesta,
                                   DataAssegnazione = g.Key.ToString("dd/MM/yyyy HH:mm - ddd"),
                                   DataAssegnazione_Ticks = g.Key.Ticks
                                 }
                    };
        await Module.InvokeVoidAsync("RapportiObj.loadPanelRender", "#LoadPanelContainer");
        await Module.InvokeVoidAsync("RapportiObj.renderGrids", objRef, Feste);
      }
      await base.OnAfterRenderAsync(firstRender);
    }
    public void Dispose()
    {
      objRef?.Dispose();
      cts.Cancel();
    }
    #endregion

    #region Metodi
    //public async void onExportXls(Microsoft.AspNetCore.Components.Web.MouseEventArgs args)
    //{
    //  var watch = System.Diagnostics.Stopwatch.StartNew();

    //  long[] nums = Array.ConvertAll(local_strAllSelected.Split(','), long.Parse);

    //  IWorkbook workbook;
    //  workbook = new XSSFWorkbook();

    //  foreach (var item in nums)
    //  {
    //    DateTime myDate = new DateTime(item);
    //    String test = myDate.ToString("ddd_dd_MM_yyyy_HH_mm");

    //    long[] selGiornate = new long[1];
    //    selGiornate[0] = item;
    //    CreaFoglio(workbook, test, local_selFesta.ToString(), selGiornate);
    //  }
    //  CreaFoglio(workbook, "Consuntivo", local_selFesta.ToString(), nums);

    //  var newFile = string.Format(@"ReportConsumi_{0}.xlsx", DateTime.Now.ToString("yyyyMMdd_HHmmss"));
    //  using (var memory = new MemoryStream())
    //  {
    //    workbook.Write(memory);
    //    byte[] bytes = memory.ToArray();

    //    await Module.InvokeVoidAsync("RapportiObj.saveAsFile", newFile, "application/vnd.ms-excel", bytes);
    //  }
    //}
    private void CreaFoglio(IWorkbook workbook, string strSheetName, int idFesta, List<long> selGiornate)
    {
      var DatiFestaInCorso = from o in local_qryOrdini.Where(w => selGiornate.Contains(w.DataAssegnazione.Ticks))
                             join r in local_qryOrdiniRighe on o.IdOrdine equals r.IdOrdine
                             group r by new { o.Cassa, r.IdProdotto } into g
                             select new
                             {
                               g.Key.Cassa,
                               g.Key.IdProdotto,
                               Ordini = g.Count(),
                               Importo = g.Where(w => w.IdProdotto == g.Key.IdProdotto).Sum(s => s.Importo),
                               Quantità = g.Where(w => w.IdProdotto == g.Key.IdProdotto).Sum(r => r.QuantitàProdotto)
                             };

      List<ReportProdotti> qryProdotti = (from pp in prodottiUsatiNellaFesta.OrderBy(k => k.IdProdotto)
                                          select new ReportProdotti
                                          {
                                            Prodotto = pp,
                                            Casse = (from c in local_Casse.OrderBy(o => o.IdCassa)
                                                     select new ReportProdotti_Casse
                                                     {
                                                       Cassa = c
                                                     }).ToList()
                                          }).ToList();

      foreach (var item in DatiFestaInCorso)
      {
        // Cerco la Cassa
        int idxProdotto = qryProdotti.FindIndex(i => i.Prodotto.IdProdotto == item.IdProdotto);
        if (idxProdotto != -1)
        {
          qryProdotti[idxProdotto].Importo += item.Importo;
          qryProdotti[idxProdotto].Quantità += item.Quantità;

          int idxCassa = qryProdotti[idxProdotto].Casse.FindIndex(x => x.Cassa.IdCassa.ToString() == item.Cassa);
          if (idxCassa != -1)
          {
            qryProdotti[idxProdotto].Casse[idxCassa].Importo = item.Importo;
            qryProdotti[idxProdotto].Casse[idxCassa].Quantità = item.Quantità;
          }
        }
      }

      ISheet excelSheet = workbook.CreateSheet(strSheetName);
      IRow row1;
      IRow row2;

      int RowIndex = 0;
      int ColumnIndex = 0;

      var titleFont = workbook.CreateFont();
      titleFont.IsBold = true;
      titleFont.FontHeightInPoints = 10;

      ICellStyle titleStyle = workbook.CreateCellStyle();
      titleStyle.SetFont(titleFont);
      titleStyle.VerticalAlignment = VerticalAlignment.Center;
      titleStyle.Alignment = HorizontalAlignment.Center;

      ICellStyle currencyCellStyle = workbook.CreateCellStyle();
      var formatId = HSSFDataFormat.GetBuiltinFormat("€ #.##0");
      if (formatId == -1)
      {
        var newDataFormat = workbook.CreateDataFormat();
        currencyCellStyle.DataFormat = newDataFormat.GetFormat("€ #.##0");
      }
      else
        currencyCellStyle.DataFormat = formatId;

      // Intestazione Report
      row1 = excelSheet.CreateRow(RowIndex++);
      row2 = excelSheet.CreateRow(RowIndex++);

      row1.CreateCell(ColumnIndex++).SetCellValue("#");
      row1.CreateCell(ColumnIndex++).SetCellValue("Prodotto");

      var cra = new NPOI.SS.Util.CellRangeAddress(0, 1, 0, 0);
      excelSheet.AddMergedRegion(cra);
      cra = new NPOI.SS.Util.CellRangeAddress(0, 1, 1, 1);
      excelSheet.AddMergedRegion(cra);

      foreach (var itemC in qryProdotti[0].Casse)
      {
        row1.CreateCell(ColumnIndex).SetCellValue(itemC.Cassa.Cassa);
        row1.CreateCell(ColumnIndex + 1);
        cra = new NPOI.SS.Util.CellRangeAddress(0, 0, ColumnIndex, ColumnIndex + 1);
        excelSheet.AddMergedRegion(cra);

        row2.CreateCell(ColumnIndex++).SetCellValue("Importo");
        row2.CreateCell(ColumnIndex++).SetCellValue("Quantità");
      }
      row1.CreateCell(ColumnIndex).SetCellValue("Totale");
      row1.CreateCell(ColumnIndex + 1);
      cra = new NPOI.SS.Util.CellRangeAddress(0, 0, ColumnIndex, ColumnIndex + 1);
      excelSheet.AddMergedRegion(cra);

      row2.CreateCell(ColumnIndex++).SetCellValue("Importo");
      row2.CreateCell(ColumnIndex++).SetCellValue("Quantità");

      for (int i = 0; i < RowIndex; i++)
      {
        for (int j = 0; j < ColumnIndex; j++)
        {
          if (excelSheet.GetRow(i).GetCell(j) != null)
            excelSheet.GetRow(i).GetCell(j).CellStyle = titleStyle;
        }
      }
      foreach (var itemP in qryProdotti)
      {
        row1 = excelSheet.CreateRow(RowIndex++);
        ColumnIndex = 0;
        row1.CreateCell(ColumnIndex++).SetCellValue(itemP.Prodotto.IdProdotto);
        row1.CreateCell(ColumnIndex++).SetCellValue(itemP.Prodotto.NomeProdotto.CR_to_Space());

        foreach (var itemC in itemP.Casse)
        {
          row1.CreateCell(ColumnIndex++).SetCellValue(itemC.Importo);
          row1.CreateCell(ColumnIndex++).SetCellValue(itemC.Quantità);
          if (row1.GetCell(ColumnIndex - 2) != null)
            row1.GetCell(ColumnIndex - 2).CellStyle = currencyCellStyle;
        }
        row1.CreateCell(ColumnIndex++).SetCellValue(itemP.Importo);
        row1.CreateCell(ColumnIndex++).SetCellValue(itemP.Quantità);
        if (row1.GetCell(ColumnIndex - 2) != null)
          row1.GetCell(ColumnIndex - 2).CellStyle = currencyCellStyle;
      }

      row1 = excelSheet.CreateRow(RowIndex++);
      row1 = excelSheet.CreateRow(RowIndex++);

      for (int i = 2; i < ColumnIndex; i++)
      {
        try
        {
          row1.CreateCell(i);
          row1.GetCell(i).SetCellType(CellType.Formula);
          row1.GetCell(i).SetCellFormula(string.Format("SUM({0}{1}:{0}{2})", (char)(65 + i), 3, RowIndex - 2));

          if (i % 2 == 0)
            row1.GetCell(i).CellStyle = currencyCellStyle;
        }
        catch (Exception ex)
        {
          Log.Error($"CreaFoglio - {ex}");
        }
      }

      XSSFFormulaEvaluator.EvaluateAllFormulaCells(workbook);

      for (int i = 0; i < ColumnIndex; i++)
      {
        excelSheet.AutoSizeColumn(i);
      }
    }
    #endregion

    #region JSInvokable
    [JSInvokable("onAggiornaStatoProdottiAsync")]
    public async Task<string> onAggiornaStatoProdottiAsync(int IdFesta, List<long> selectedDays)
    {
      List<AnagrCasse> local_anagrCasse = new List<AnagrCasse>();

      var watch = System.Diagnostics.Stopwatch.StartNew();

      local_selFesta = IdFesta;

      ArchFeste DatiFesta = local_archFeste.Where(w => w.IdFesta == IdFesta).FirstOrDefault();

      local_qryOrdiniRighe = (await festeDataAccess.GetGenericQuery<ArchOrdiniRighe>("SELECT r.* FROM arch_ordini o JOIN arch_ordini_righe r ON o.IdOrdine = r.IdOrdine WHERE o.IdFesta = @IdFesta", new { IdFesta = DatiFesta.IdFesta })).ToList();

      // Recupero l'elenco dei prodotti usati nella festa selezionata
      prodottiUsatiNellaFesta = (from r in local_qryOrdiniRighe
                                 group r by new { r.IdProdotto, r.NomeProdotto } into g
                                 select new AnagrProdotti
                                 {
                                   IdProdotto = g.Key.IdProdotto,
                                   NomeProdotto = g.Key.NomeProdotto,
                                   PrezzoUnitario = g.Sum(s => s.Importo) / g.Sum(s => s.QuantitàProdotto),
                                   Stato = true
                                 }).ToList();

      local_anagrCasse = (await festeDataAccess.GetGenericQuery<AnagrCasse>($"SELECT * FROM anagr_casse")).ToList();

      var CasseUsateNellaFesta = from o in local_qryOrdini.Where(w => w.IdFesta == DatiFesta.IdFesta)
                                 group o by o.IdCassa into g
                                 select new { IdCassa = g.Key };

      local_Casse = (from c in local_anagrCasse.Where(w => w.IdListino == DatiFesta.IdListino)
                     join u in CasseUsateNellaFesta on c.IdCassa equals u.IdCassa
                     select c).ToList();

      var DatiFestaInCorso = from o in local_qryOrdini.Where(w => selectedDays.Contains(w.DataAssegnazione.Ticks))
                             join r in local_qryOrdiniRighe on o.IdOrdine equals r.IdOrdine
                             join p in prodottiUsatiNellaFesta on r.IdProdotto equals p.IdProdotto
                             select new { o, r, p };

      var _qryStatoCasse = (from r in DatiFestaInCorso
                            group r by new { r.o.IdCassa, r.r.IdProdotto } into g
                            orderby g.Key.IdCassa
                            select new
                            {
                              IdCassa = g.Key.IdCassa,
                              IdProdotto = g.Key.IdProdotto,
                              Importo = g.Sum(s => s.r.Importo),
                              QuantitàProdotto = g.Sum(s => s.r.QuantitàProdotto),
                            });

      var _qryStatoProdotti = (from r in DatiFestaInCorso
                               group r by r.r.IdProdotto into g
                               orderby g.Key
                               select new
                               {
                                 IdProdotto = g.Key,
                                 Importo = g.Sum(s => s.r.Importo),
                                 QuantitàProdotto = g.Sum(s => s.r.QuantitàProdotto),
                               });

      var _qryStatoOrdini = (from p in prodottiUsatiNellaFesta
                             join r in _qryStatoProdotti on p.IdProdotto equals r.IdProdotto
                             orderby p.IdProdotto
                             select new
                             {
                               IdProdotto = p.IdProdotto,
                               NomeProdotto = p.NomeProdotto.CR_to_Space(),
                               Importo = r.Importo,
                               Quantità = r.QuantitàProdotto,
                               statoCassa = (from c in _qryStatoCasse
                                             group c by c.IdCassa into g
                                             orderby g.Key
                                             select new StatoCasse
                                             {
                                               IdCassa = g.Key,
                                               Importo = g.Where(c => c.IdProdotto == p.IdProdotto).Sum(s => s.Importo),
                                               QuantitàProdotto = g.Where(c => c.IdProdotto == p.IdProdotto).Sum(s => s.QuantitàProdotto)
                                             }
                                             ).ToList(),
                             }).ToList();

      Dictionary<string, object> sendDict = new Dictionary<string, object>
      {
        { "statoCasse", local_Casse },
        { "statoOrdini", _qryStatoOrdini }
      };
      watch.Stop();

      EsportaDisabled = ((local_selFesta == -1) || (local_strAllSelected == ""));
      await InvokeAsync(StateHasChanged);

      return (JsonConvert.SerializeObject(sendDict, new JsonSerializerSettings { ContractResolver = contractResolver }));
    }

    [JSInvokable("onExportingAsync")]
    public async Task onExportingAsync(int IdFesta, List<long> selectedDays)
    {
      await Module.InvokeVoidAsync("RapportiObj.loadPanelSetMessage", "Creazione report in corso...");
      await Module.InvokeVoidAsync("RapportiObj.loadPanelSetVisible", true);

      try
      {
        IWorkbook workbook;
        workbook = new XSSFWorkbook();

        CreaFoglio(workbook, "Consuntivo", IdFesta, selectedDays);
        foreach (var item in selectedDays)
        {
          DateTime myDate = new DateTime(item);
          String test = myDate.ToString("ddd_dd_MM_yyyy_HH_mm");

          List<long> selGiornate = new List<long>();
          selGiornate.Add(item);
          CreaFoglio(workbook, test, IdFesta, selGiornate);
        }

        #region File Download
        var newFile = string.Format(@"ReportConsumi_{0}.xlsx", DateTime.Now.ToString("yyyyMMdd_HHmmss"));
        using (var memory = new MemoryStream())
        {
          workbook.Write(memory, false);
          byte[] bytes = memory.ToArray();

          if (!cts.IsCancellationRequested)
            await Module.InvokeVoidAsync("RapportiObj.downloadFileFromStream", newFile, bytes);
        }
        #endregion
        workbook.Close();
      }
      catch (Exception ex)
      {
        _ = ex;
      }
      await Module.InvokeVoidAsync("RapportiObj.loadPanelSetVisible", false);
    }
    #endregion
  }
}
