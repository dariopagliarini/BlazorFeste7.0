using BlazorFeste.Data.Models;

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

#if THREADSAFE
    public ConcurrentDictionary<int, AnagrProdotti> AnagrProdotti { get; set; } = new ConcurrentDictionary<int, AnagrProdotti>();
    public ConcurrentDictionary<long, ArchOrdini> QryOrdini { get; set; } = new ConcurrentDictionary<long, ArchOrdini>();
    public ConcurrentDictionary<Tuple<long, int>, ArchOrdiniRighe> QryOrdiniRighe { get; set; } = new ConcurrentDictionary<Tuple<long, int>, ArchOrdiniRighe>();
#else
    public List<AnagrProdotti> AnagrProdotti { get; set; } = new List<AnagrProdotti>();
    public List<ArchOrdini> QryOrdini { get; set; } = new List<ArchOrdini>();
    public List<ArchOrdiniRighe> QryOrdiniRighe { get; set; } = new List<ArchOrdiniRighe>();
#endif

    public long elapsed_GetDatabaseData { get; set; }

    public event EventHandler<DateTime> DataOraServer = default!;
    public event EventHandler<string> UpdateListe;
    public event EventHandler<long> NotifyStatoOrdine;
    public event EventHandler<int> NotifyStatoProdotti;
    public event EventHandler<int> NotifyStatoLista;
    public event EventHandler<bool> NotifyAnagrProdotti;
    public event EventHandler<bool> NotifyAnagrCasse;
    public event EventHandler<bool> NotifyAnagrListe;
    #endregion

    public UserInterfaceService(IWebHostEnvironment iWebHostEnvironment)
    {
      _IWebHostEnvironment = iWebHostEnvironment;
    }
    public void OnDataOraServer(DateTime adesso)
    {
      DataOraServer?.Invoke(this, adesso);
    }
    public void OnUpdateListe(string ElapsedInfo)
    {
      UpdateListe?.Invoke(this, ElapsedInfo);
    }
    public void OnNotifyStatoOrdine(long idOrdine)
    {
      NotifyStatoOrdine?.Invoke(this, idOrdine);
    }
    public void OnNotifyStatoProdotti(int idCassa)
    {
      NotifyStatoProdotti?.Invoke(this, idCassa);
    }
    public void OnNotifyStatoLista(int idLista)
    {
      NotifyStatoLista?.Invoke(this, idLista);
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
      DateTime dtOggi;

      //if (_IWebHostEnvironment.IsDevelopment())
      //{
      //  dtOggi = DateTime.Parse("2022-06-12 22:00:00"); // dtOggi = DateTime.Parse("2021-07-31 12:00:00");
      //}
      //else
      {
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
      }
      //dtOggi = DateTime.Parse("2019-07-01 22:00:00"); // dtOggi = DateTime.Parse("2021-07-31 12:00:00");
      return (dtOggi);
    }
  }
}

