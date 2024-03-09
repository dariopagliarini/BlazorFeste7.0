using BlazorFeste.Data.Models;

using NPOI.SS.Formula.Functions;

using System.Collections.Concurrent;

namespace BlazorFeste.Services
{
  public class UserInterfaceService
  {
    #region Variabili
    private readonly IWebHostEnvironment _IWebHostEnvironment;

    public DateTime DtFestaInCorso { get; set; }
    public ArchFeste ArchFesta { get; set; }
    public List<AnagrCasse> AnagrCasse { get; set; } = new List<AnagrCasse>();
    public List<AnagrListe> AnagrListe { get; set; } = new List<AnagrListe>();
    public List<AnagrStampanti> AnagrStampanti { get; set; } = new List<AnagrStampanti>();

    public ConcurrentDictionary<int, AnagrProdotti> AnagrProdotti { get; set; } = new ConcurrentDictionary<int, AnagrProdotti>();
    public ConcurrentDictionary<long, ArchOrdini> QryOrdini { get; set; } = new ConcurrentDictionary<long, ArchOrdini>();
    public ConcurrentDictionary<Tuple<long, int>, ArchOrdiniRighe> QryOrdiniRighe { get; set; } = new ConcurrentDictionary<Tuple<long, int>, ArchOrdiniRighe>();

    public long updatesQryOrdini { get; set; } = 0;
    public long updatesQryOrdiniRighe { get; set; } = 0;

    public event EventHandler<DateTime> NotifyDataOraServer = default!;
    public event EventHandler<string> NotifyUpdateListe;
    public event EventHandler<long> NotifyStatoOrdine;
    public event EventHandler<DatiNotifyStatoProdotti> NotifyStatoProdotti;
    public event EventHandler<DatiNotifyStatoLista> NotifyStatoLista;
    public event EventHandler<bool> NotifyAnagrProdotti;
    public event EventHandler<bool> NotifyAnagrCasse;
    public event EventHandler<bool> NotifyAnagrListe;

    public event EventHandler<DatiOrdine> NotifyNuovoOrdine;
    public event EventHandler<DatiNotifyDashboard> NotifyDashboard;
    public int NotifyDashboardCount {
      get
      {
        int iCount = 0;
        if (NotifyDashboard != null)
        {
          iCount = NotifyDashboard.GetInvocationList().Length;
        }
        return iCount;
      }
    }
    #endregion

    public UserInterfaceService(IWebHostEnvironment iWebHostEnvironment)
    {
      _IWebHostEnvironment = iWebHostEnvironment;
    }
    public void OnNotifyDataOraServer(DateTime adesso)
    {
      NotifyDataOraServer?.Invoke(this, adesso);
    }
    public void OnNotifyUpdateListe(string ElapsedInfo)
    {
      NotifyUpdateListe?.Invoke(this, ElapsedInfo);
    }
    public void OnNotifyStatoOrdine(long idOrdine)
    {
      NotifyStatoOrdine?.Invoke(this, idOrdine);
    }
    public void OnNotifyNuovoOrdine(DatiOrdine datiOrdine)
    {
      NotifyNuovoOrdine?.Invoke(this, datiOrdine);
    }
    public void OnNotifyDashboard(DatiNotifyDashboard datiDashboard)
    {
      NotifyDashboard?.Invoke(this, datiDashboard);
    }
  public void OnNotifyStatoProdotti(DatiNotifyStatoProdotti datiNotifyStatoProdotti)
    {
      NotifyStatoProdotti?.Invoke(this, datiNotifyStatoProdotti);
    }
    public void OnNotifyStatoLista(DatiNotifyStatoLista datiNotifyStatoLista)
    {
      NotifyStatoLista?.Invoke(this, datiNotifyStatoLista);
    }
    public void OnNotifyAnagrProdotti(bool refresh)
    {
      NotifyAnagrProdotti?.Invoke(this, refresh);
    }
    public void OnNotifyAnagrCasse(bool refresh)
    {
      NotifyAnagrCasse?.Invoke(this, refresh);
    }
    public void OnNotifyAnagrListe(bool refresh)
    {
      NotifyAnagrListe?.Invoke(this, refresh);
    }
    public DateTime GetCurrentDataAssegnazione()
    {
      DateTime dtOggi = DateTime.Now.Date.AddDays(-1).AddHours(22);

      switch (DateTime.Now.Hour)
      {
        case 8:
        case 9:
        case 10:
        case 11:
        case 12:
        case 13:
        case 14:
        case 15:
        case 16:
          dtOggi = DateTime.Now.Date.AddHours(12);
          break;

        case 17:
        case 18:
        case 19:
        case 20:
        case 21:
        case 22:
        case 23:
          dtOggi = DateTime.Now.Date.AddHours(22);
          break;

        default:
          dtOggi = DateTime.Now.Date.AddDays(-1).AddHours(22);
          break;
      }
      // dtOggi = DateTime.Parse("2023-06-19 22:00:00"); 
      //dtOggi = DateTime.Parse("2023-06-19 22:00:00"); 

      return (dtOggi);
    }
  }
}

