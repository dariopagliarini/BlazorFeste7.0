using BlazorFeste.Data.Models;
using BlazorFeste.Util;

using Serilog;

namespace BlazorFeste.lib
{
  public class GestioneScontrini_Ordini
  {
    public const int K_LINE_FEEDS = 5;

    public const int K_LARGH_QUANTITA = 5;
    public const int K_LARGH_IMPORTO = 7;
    public const int K_LARGH_RIGA = 42; 

    public const int K_LARGH_PRODOTTO = K_LARGH_RIGA - (K_LARGH_QUANTITA + K_LARGH_IMPORTO + 2);

    public byte[] Prepara_StampaDiProva(string _nomeGiornata, ArchFeste _festa, AnagrCasse _cassa)
    {
      byte[] ret;

      using (MemoryStream memoryStream = new MemoryStream())
      {
        //Printer init
        ThermalMemoryStream memoryStreamPrinter = new ThermalMemoryStream(memoryStream, 4, 80, 2);
        memoryStreamPrinter.WakeUp();

        #region Stampa Ricevuta
        Stampa_Intestazione(_nomeGiornata, _festa, null, _cassa, memoryStreamPrinter, false);
        memoryStreamPrinter.LineFeed(K_LINE_FEEDS);
        memoryStreamPrinter.CutRequest();
        #endregion

        memoryStreamPrinter.Sleep();
        ret = memoryStream.ToArray();

        memoryStream.Close();
      }
      return (ret);
    }

    public byte[] Prepara_Ordine(StampaOrdine_Scontrino stampaOrdine)
    {
      byte[] ret;

      //Serial port init
      using (MemoryStream memoryStream = new MemoryStream()) //  (stampaOrdine.Cassa.Stampante, 38400))
      {
        //Printer init
        ThermalMemoryStream memoryStreamPrinter = new ThermalMemoryStream(memoryStream, 4, 80, 2);
        memoryStreamPrinter.WakeUp();

        #region Stampa Ricevuta
        Stampa_Intestazione(stampaOrdine.strNomeGiornata, stampaOrdine.Festa, stampaOrdine.Ordine, stampaOrdine.Cassa, memoryStreamPrinter);

        // Se "SERVITO" stampa tavolo e coperti
        memoryStreamPrinter.SetAlignCenter();
        if (stampaOrdine.Ordine.TipoOrdine == "SERVITO")
        {
          memoryStreamPrinter.WriteLine($"Tav {stampaOrdine.Ordine.Tavolo} - Coperti {(stampaOrdine.Ordine.NumeroCoperti == "" ? "0" : stampaOrdine.Ordine.NumeroCoperti)} - {stampaOrdine.Ordine.Referente}".Truncate(K_LARGH_RIGA), (byte)ThermalMemoryStream.PrintingStyle.Bold);
          memoryStreamPrinter.WriteLine("Ordine Servito", (byte)ThermalMemoryStream.PrintingStyle.Bold);
        }
        else
        {
          memoryStreamPrinter.WriteLine($"{stampaOrdine.Ordine.Referente}".Truncate(K_LARGH_RIGA), (byte)ThermalMemoryStream.PrintingStyle.Bold);
          memoryStreamPrinter.WriteLine("Buono Ritiro", (byte)ThermalMemoryStream.PrintingStyle.Bold);
        }
        // Gestione della stampa della Ricevuta
        memoryStreamPrinter.WriteLine("RICEVUTA", (byte)ThermalMemoryStream.PrintingStyle.Bold + (byte)ThermalMemoryStream.PrintingStyle.DoubleHeight);
        memoryStreamPrinter.SetAlignCenter();
        memoryStreamPrinter.HorizontalLine(K_LARGH_RIGA);

        memoryStreamPrinter.SetAlignLeft();
        memoryStreamPrinter.WriteLine($"Euro".PadLeft(K_LARGH_RIGA)); //, (byte)ThermalMemoryStream.PrintingStyle.Bold);

        double total = 0;
        foreach (var item in stampaOrdine.RigheOrdine.OrderBy(o => o.IdProdotto))
        {
          double _importo = stampaOrdine.ScontrinoMuto ? 0 : item.Importo;

          memoryStreamPrinter.WriteLine($"{item.QuantitàProdotto.ToString().PadLeft(K_LARGH_QUANTITA).ToUpper()} {(item.NomeProdotto.Length > K_LARGH_PRODOTTO ? item.NomeProdotto.Substring(0, K_LARGH_PRODOTTO-1) + "." : item.NomeProdotto.PadRight(K_LARGH_PRODOTTO))} {$"{_importo:0.00}".PadLeft(K_LARGH_IMPORTO)}".ToUpper(),
           (byte)ThermalMemoryStream.PrintingStyle.Bold);
          total += _importo;
        }
        memoryStreamPrinter.SetAlignCenter();
        memoryStreamPrinter.HorizontalLine(K_LARGH_RIGA);

        memoryStreamPrinter.SetAlignLeft();
        memoryStreamPrinter.WriteLine($"Totale: {total:0.00}".PadLeft(K_LARGH_RIGA), (byte)ThermalMemoryStream.PrintingStyle.Bold);

        Stampa_PièDiPagina(stampaOrdine.Festa, stampaOrdine.Ordine, memoryStreamPrinter);
        #endregion

        #region Stampa Categorie
        // Per ogni Categoria stampo uno scontrino diverso
        foreach (var item in stampaOrdine.ListeDaStampare.OrderBy(o => o.IdLista))
        {
          Stampa_Intestazione(stampaOrdine.strNomeGiornata, stampaOrdine.Festa, stampaOrdine.Ordine, stampaOrdine.Cassa, memoryStreamPrinter);

          // Se "SERVITO" stampa tavolo e coperti
          memoryStreamPrinter.SetAlignCenter();
          if (stampaOrdine.Ordine.TipoOrdine == "SERVITO")
          {
            memoryStreamPrinter.WriteLine($"Tav {stampaOrdine.Ordine.Tavolo} - Coperti {(stampaOrdine.Ordine.NumeroCoperti == "" ? "0" : stampaOrdine.Ordine.NumeroCoperti)} - {stampaOrdine.Ordine.Referente}".Truncate(K_LARGH_RIGA), (byte)ThermalMemoryStream.PrintingStyle.Bold);
            //printer.WriteLine("Ordine Servito", (byte)ThermalMemoryStream.PrintingStyle.Bold);
          }
          else
          {
            memoryStreamPrinter.WriteLine($"{stampaOrdine.Ordine.Referente}".Truncate(K_LARGH_RIGA), (byte)ThermalMemoryStream.PrintingStyle.Bold);
            //printer.WriteLine("Buono Ritiro", (byte)ThermalMemoryStream.PrintingStyle.Bold);
          }

          // Gestione della stampa della Ricevuta della Categoria
          memoryStreamPrinter.WriteLine($"{item.Lista.ToUpper()}", (byte)ThermalMemoryStream.PrintingStyle.Bold + (byte)ThermalMemoryStream.PrintingStyle.DoubleHeight);
          memoryStreamPrinter.SetAlignCenter();
          memoryStreamPrinter.HorizontalLine(K_LARGH_RIGA);
          memoryStreamPrinter.SetAlignLeft();
          foreach (var itemR in stampaOrdine.RigheOrdine.Where(w => w.IdCategoria == item.IdLista).OrderBy(o => o.IdProdotto))
          {
            double _importo = stampaOrdine.ScontrinoMuto ? 0 : itemR.Importo;

            memoryStreamPrinter.WriteLine($"{itemR.QuantitàProdotto.ToString().PadLeft(K_LARGH_QUANTITA).ToUpper()} {(itemR.NomeProdotto.Length > K_LARGH_PRODOTTO ? itemR.NomeProdotto.Substring(0, K_LARGH_PRODOTTO-1) + "." : itemR.NomeProdotto.PadRight(K_LARGH_PRODOTTO))} {$"{_importo:0.00}".PadLeft(K_LARGH_IMPORTO)}".ToUpper(),
             (byte)ThermalMemoryStream.PrintingStyle.Bold);
          }

          try
          {
            if ((item.StampaNoteOrdine.Value) && (stampaOrdine.Ordine.NoteOrdine.Length > 0))
            {
              memoryStreamPrinter.SetAlignCenter();
              memoryStreamPrinter.HorizontalLine(K_LARGH_RIGA);
              memoryStreamPrinter.SetAlignLeft();

              IEnumerable<string> _NoteOrdine = stampaOrdine.Ordine.NoteOrdine.ToUpper().Split(K_LARGH_RIGA);
              foreach (var _NotaOrdine in _NoteOrdine)
              {
                memoryStreamPrinter.WriteLine($"{_NotaOrdine}".Truncate(K_LARGH_RIGA), (byte)ThermalMemoryStream.PrintingStyle.Bold);
              }
            }
          }
          catch (Exception ex)
          {
            Log.Error($"GestioneScontrini_Ordine - Prepara_Ordine - {ex}");
          }

          memoryStreamPrinter.SetAlignCenter();
          memoryStreamPrinter.HorizontalLine(K_LARGH_RIGA);
          memoryStreamPrinter.LineFeed(K_LINE_FEEDS);
          memoryStreamPrinter.CutRequest();
        }
        #endregion

        #region Stampa Queue Ticket
        memoryStreamPrinter.SetAlignCenter();
        foreach (var item in stampaOrdine.QueueTicketDaStampare.OrderBy(o => o.IdProdotto))
        {
          Stampa_Intestazione(stampaOrdine.strNomeGiornata, stampaOrdine.Festa, stampaOrdine.Ordine, stampaOrdine.Cassa, memoryStreamPrinter);

          memoryStreamPrinter.WriteLine($"{stampaOrdine.Ordine.Referente}".Truncate(K_LARGH_RIGA), (byte)ThermalMemoryStream.PrintingStyle.Bold);
          memoryStreamPrinter.WriteLine($"{item.NomeProdotto.ToUpper()}", (byte)ThermalMemoryStream.PrintingStyle.Bold + (byte)ThermalMemoryStream.PrintingStyle.DoubleHeight);
          memoryStreamPrinter.HorizontalLine(K_LARGH_RIGA);
          memoryStreamPrinter.LineFeed(1);
          memoryStreamPrinter.WriteLine($"#{item.QueueTicket}".ToUpper(), (byte)ThermalMemoryStream.PrintingStyle.Bold + (byte)ThermalMemoryStream.PrintingStyle.DoubleHeight + (byte)ThermalMemoryStream.PrintingStyle.DoubleWidth);
          memoryStreamPrinter.LineFeed(1);
          memoryStreamPrinter.HorizontalLine(K_LARGH_RIGA);
          memoryStreamPrinter.LineFeed(K_LINE_FEEDS);
          memoryStreamPrinter.CutRequest();
        }
        #endregion

        memoryStreamPrinter.Sleep();
        ret = memoryStream.ToArray();
        memoryStream.Close();

        // Log.Information($"Id: {stampaOrdine.Cassa.OrdiniDellaCassa} - Cassa {stampaOrdine.Ordine.Cassa} - Prepara Scontrino - Ordine #{stampaOrdine.Ordine.ProgressivoSerata} [{stampaOrdine.Ordine.IdOrdine}]");
      }
      return (ret);
    }
    public byte[] Prepara_Ordine_Cucina(StampaOrdine_ScontrinoCucina stampaOrdine)
    {
      byte[] ret;

      using (MemoryStream memoryStream = new MemoryStream())
      {
        //Printer init
        ThermalMemoryStream memoryStreamPrinter = new ThermalMemoryStream(memoryStream, 4, 80, 2);
        memoryStreamPrinter.WakeUp();

        #region Stampa Categorie
        for (int i = 1; i <= stampaOrdine.ListaDaStampare.Cucina_NumeroScontrini; i++)
        {
          memoryStreamPrinter.SetAlignCenter();

          memoryStreamPrinter.WriteLine($"{stampaOrdine.strNomeGiornata}", (byte)ThermalMemoryStream.PrintingStyle.Bold);
          memoryStreamPrinter.WriteLine($"{stampaOrdine.Cassa.Cassa} - {stampaOrdine.Ordine.DataOra.ToString("dd/MM/yyyy HH:mm:ss")}".Truncate(K_LARGH_RIGA),
            (byte)ThermalMemoryStream.PrintingStyle.Bold + (byte)ThermalMemoryStream.PrintingStyle.Underline);
          memoryStreamPrinter.HorizontalLine(K_LARGH_RIGA);

          memoryStreamPrinter.SetAlignLeft();
          memoryStreamPrinter.WriteLine($"Tavolo   Coperti   Sig.".Truncate(K_LARGH_RIGA),
            (byte)ThermalMemoryStream.PrintingStyle.Bold);
          memoryStreamPrinter.SetAlignCenter();
          memoryStreamPrinter.HorizontalLine(K_LARGH_RIGA);

          memoryStreamPrinter.SetAlignLeft();
          memoryStreamPrinter.WriteLine($"{stampaOrdine.Ordine.Tavolo.ToString().PadCenter(7)} {(stampaOrdine.Ordine.NumeroCoperti == "" ? "   0   " : stampaOrdine.Ordine.NumeroCoperti.PadCenter(9))}  {stampaOrdine.Ordine.Referente}".Truncate(K_LARGH_RIGA),
            (byte)ThermalMemoryStream.PrintingStyle.Bold +
            (byte)ThermalMemoryStream.PrintingStyle.DoubleHeight);

          memoryStreamPrinter.SetAlignCenter();
          memoryStreamPrinter.HorizontalLine(K_LARGH_RIGA);

          // Gestione della stampa della Ricevuta della Categoria
          memoryStreamPrinter.SetAlignRight();
          memoryStreamPrinter.WriteLine($"{stampaOrdine.ListaDaStampare.Lista.ToUpper()}",
            (byte)ThermalMemoryStream.PrintingStyle.Bold);

          memoryStreamPrinter.SetAlignLeft();
          foreach (var itemR in stampaOrdine.RigheOrdine.Where(w => w.IdCategoria == stampaOrdine.ListaDaStampare.IdLista).OrderBy(o => o.IdProdotto))
          {
            memoryStreamPrinter.WriteLine($"{itemR.QuantitàProdotto.ToString().PadLeft(K_LARGH_QUANTITA).ToUpper()} {(itemR.NomeProdotto.Length > K_LARGH_PRODOTTO ? itemR.NomeProdotto.Substring(0, K_LARGH_PRODOTTO-1) + "." : itemR.NomeProdotto.PadRight(K_LARGH_PRODOTTO))} {$"{itemR.Importo:0.00}".PadLeft(K_LARGH_IMPORTO)}".ToUpper(),
             (byte)ThermalMemoryStream.PrintingStyle.Bold);
          }
          memoryStreamPrinter.SetAlignCenter();
          memoryStreamPrinter.HorizontalLine(K_LARGH_RIGA);

          if (stampaOrdine.ListaDaStampare.Cucina_NumeroScontrini > 1)
          {
            memoryStreamPrinter.WriteLine($"#{stampaOrdine.Ordine.IdOrdine} / {i}", (byte)ThermalMemoryStream.PrintingStyle.Bold);
            memoryStreamPrinter.HorizontalLine(K_LARGH_RIGA);
          }

          try
          {
            if ((stampaOrdine.ListaDaStampare.StampaNoteOrdine.Value) && (stampaOrdine.Ordine.NoteOrdine.Length > 0))
            {
              memoryStreamPrinter.SetAlignCenter();
              memoryStreamPrinter.HorizontalLine(K_LARGH_RIGA);
              memoryStreamPrinter.SetAlignLeft();

              IEnumerable<string> _NoteOrdine = stampaOrdine.Ordine.NoteOrdine.ToUpper().Split(K_LARGH_RIGA);
              foreach (var _NotaOrdine in _NoteOrdine)
              {
                memoryStreamPrinter.WriteLine($"{_NotaOrdine}".Truncate(K_LARGH_RIGA), (byte)ThermalMemoryStream.PrintingStyle.Bold);
              }
            }
          }
          catch (Exception ex)
          {
            Log.Error($"GestioneScontrini_Ordine - Prepara_Ordine_Cucina - {ex}");
          }

          //memoryStreamPrinter.SetAlignCenter();
          //memoryStreamPrinter.HorizontalLine(K_LARGH_RIGA);
          memoryStreamPrinter.LineFeed(K_LINE_FEEDS);
          memoryStreamPrinter.CutRequest();
        }
        #endregion

        memoryStreamPrinter.Sleep();
        ret = memoryStream.ToArray();
        memoryStream.Close();

        //Log.Information($"Id: {stampaOrdine.Cassa.OrdiniDellaCassa} - Cassa {stampaOrdine.Ordine.Cassa} - Prepara Scontrino Cucina - Ordine #{stampaOrdine.Ordine.ProgressivoSerata} [{stampaOrdine.Ordine.IdOrdine}]");
      }
      return (ret);
    }
    public byte[] Prepara_Consumi(StampaOrdine_ConsumiGiornata consumiGiornata)
    {
      byte[] ret;

      using (MemoryStream memoryStream = new MemoryStream())
      {
        //Printer init
        ThermalMemoryStream memoryStreamPrinter = new ThermalMemoryStream(memoryStream, 4, 80, 2);
        memoryStreamPrinter.WakeUp();

        #region Stampa Ricevuta
        if (consumiGiornata.flagCumulativo)
        {
          consumiGiornata.Cassa.IdCassa = 0;
        }
        Stampa_Intestazione(consumiGiornata.strNomeGiornata, consumiGiornata.Festa, null, consumiGiornata.Cassa, memoryStreamPrinter, false);

        // Gestione della stampa del consuntivo Consumi 
        memoryStreamPrinter.WriteLine("REPORT CONSUMI", (byte)ThermalMemoryStream.PrintingStyle.Bold + (byte)ThermalMemoryStream.PrintingStyle.DoubleHeight);
        memoryStreamPrinter.WriteLine($"Ora Stampa: {DateTime.Now:dd/MM/yyyy HH:mm:ss}", (byte)ThermalMemoryStream.PrintingStyle.Bold);
        memoryStreamPrinter.SetAlignCenter();
        memoryStreamPrinter.HorizontalLine(K_LARGH_RIGA);

        memoryStreamPrinter.SetAlignRight();
        memoryStreamPrinter.WriteLine("Euro ", (byte)ThermalMemoryStream.PrintingStyle.Bold);
        memoryStreamPrinter.SetAlignLeft();

        double total = 0;
        foreach (var item in consumiGiornata.statoCassa.OrderBy(o => o.IdProdotto))
        {
          memoryStreamPrinter.WriteLine($"{item.Quantità.ToString().PadLeft(K_LARGH_QUANTITA).ToUpper()} {(item.NomeProdotto.Length > K_LARGH_PRODOTTO ? item.NomeProdotto.Substring(0, K_LARGH_PRODOTTO - 1) + "." : item.NomeProdotto.PadRight(K_LARGH_PRODOTTO))} {$"{item.Importo:0.00}".PadLeft(K_LARGH_IMPORTO)}".ToUpper(),
           (byte)ThermalMemoryStream.PrintingStyle.Bold);
          total += item.Importo;
        }
        memoryStreamPrinter.SetAlignCenter();
        memoryStreamPrinter.HorizontalLine(K_LARGH_RIGA);
        memoryStreamPrinter.WriteLine($"Totale: {total:0.00}".PadLeft(K_LARGH_RIGA), (byte)ThermalMemoryStream.PrintingStyle.Bold);

        memoryStreamPrinter.LineFeed(K_LINE_FEEDS);
        memoryStreamPrinter.CutRequest();
        #endregion

        memoryStreamPrinter.Sleep();
        ret = memoryStream.ToArray();
        memoryStream.Close();
      }
      return (ret);
    }
    public byte[] Prepara_Consumi_Lista(StampaOrdine_ConsumiGiornata consumiGiornata)
    {
      byte[] ret;

      using (MemoryStream memoryStream = new MemoryStream())
      {
        //Printer init
        ThermalMemoryStream memoryStreamPrinter = new ThermalMemoryStream(memoryStream, 4, 80, 2);
        memoryStreamPrinter.WakeUp();

        #region Stampa Ricevuta con Importi
        memoryStreamPrinter.SetAlignCenter();
        memoryStreamPrinter.WriteLine(consumiGiornata.Festa.Associazione, (byte)ThermalMemoryStream.PrintingStyle.Bold + (byte)ThermalMemoryStream.PrintingStyle.DoubleHeight);
        memoryStreamPrinter.WriteLine(consumiGiornata.Festa.Festa, (byte)ThermalMemoryStream.PrintingStyle.Bold + (byte)ThermalMemoryStream.PrintingStyle.DoubleHeight);

        memoryStreamPrinter.WriteLine($"Cumulativo - {consumiGiornata.strNomeGiornata}", (byte)ThermalMemoryStream.PrintingStyle.Bold);
        memoryStreamPrinter.WriteLine($"{consumiGiornata.Lista.Lista}", (byte)ThermalMemoryStream.PrintingStyle.Bold);

        memoryStreamPrinter.HorizontalLine(K_LARGH_RIGA);

        // Gestione della stampa del consuntivo Consumi 
        memoryStreamPrinter.WriteLine("REPORT CONSUMI", (byte)ThermalMemoryStream.PrintingStyle.Bold + (byte)ThermalMemoryStream.PrintingStyle.DoubleHeight);
        memoryStreamPrinter.WriteLine($"Ora Stampa: {DateTime.Now:dd/MM/yyyy HH:mm:ss}", (byte)ThermalMemoryStream.PrintingStyle.Bold);
        memoryStreamPrinter.SetAlignCenter();
        memoryStreamPrinter.HorizontalLine(K_LARGH_RIGA);

        //memoryStreamPrinter.SetAlignRight();
        //memoryStreamPrinter.WriteLine("Euro ",
        memoryStreamPrinter.SetAlignLeft();
        memoryStreamPrinter.WriteLine($"{"Euro".ToString().PadLeft(K_LARGH_RIGA)}",
          (byte)ThermalMemoryStream.PrintingStyle.Bold);
        //memoryStreamPrinter.SetAlignLeft();

        double total = 0;
        foreach (var item in consumiGiornata.statoLista.OrderBy(o => o.IdProdotto))
        {
          memoryStreamPrinter.WriteLine($"{item.Quantità.ToString().PadLeft(K_LARGH_QUANTITA).ToUpper()} {(item.NomeProdotto.Length > K_LARGH_PRODOTTO ? item.NomeProdotto.Substring(0, K_LARGH_PRODOTTO - 1) + "." : item.NomeProdotto.PadRight(K_LARGH_PRODOTTO))} {$"{item.Importo:0.00}".PadLeft(K_LARGH_IMPORTO)}".ToUpper(),
           (byte)ThermalMemoryStream.PrintingStyle.Bold);
          total += item.Importo;
        }
        memoryStreamPrinter.SetAlignCenter();
        memoryStreamPrinter.HorizontalLine(K_LARGH_RIGA);

        memoryStreamPrinter.SetAlignLeft();
        memoryStreamPrinter.WriteLine($"Totale: {total:0.00}".PadLeft(K_LARGH_RIGA), (byte)ThermalMemoryStream.PrintingStyle.Bold);

        memoryStreamPrinter.LineFeed(K_LINE_FEEDS);
        memoryStreamPrinter.CutRequest();
        #endregion

        #region Stampa Ricevuta senza Importi
        memoryStreamPrinter.SetAlignCenter();
        memoryStreamPrinter.WriteLine(consumiGiornata.Festa.Associazione, (byte)ThermalMemoryStream.PrintingStyle.Bold + (byte)ThermalMemoryStream.PrintingStyle.DoubleHeight);
        memoryStreamPrinter.WriteLine(consumiGiornata.Festa.Festa, (byte)ThermalMemoryStream.PrintingStyle.Bold + (byte)ThermalMemoryStream.PrintingStyle.DoubleHeight);

        memoryStreamPrinter.WriteLine($"Cumulativo - {consumiGiornata.strNomeGiornata}", (byte)ThermalMemoryStream.PrintingStyle.Bold);
        memoryStreamPrinter.WriteLine($"{consumiGiornata.Lista.Lista}", (byte)ThermalMemoryStream.PrintingStyle.Bold);

        memoryStreamPrinter.HorizontalLine(K_LARGH_RIGA);

        // Gestione della stampa del consuntivo Consumi 
        memoryStreamPrinter.WriteLine("REPORT CONSUMI", (byte)ThermalMemoryStream.PrintingStyle.Bold + (byte)ThermalMemoryStream.PrintingStyle.DoubleHeight);
        memoryStreamPrinter.WriteLine($"Ora Stampa: {DateTime.Now:dd/MM/yyyy HH:mm:ss}", (byte)ThermalMemoryStream.PrintingStyle.Bold);
        memoryStreamPrinter.SetAlignCenter();
        memoryStreamPrinter.HorizontalLine(K_LARGH_RIGA);

        memoryStreamPrinter.SetAlignLeft();
        foreach (var item in consumiGiornata.statoLista.OrderBy(o => o.IdProdotto))
        {
          memoryStreamPrinter.WriteLine($"{item.Quantità.ToString().PadLeft(K_LARGH_QUANTITA).ToUpper()} {(item.NomeProdotto.Length > K_LARGH_PRODOTTO ? item.NomeProdotto.Substring(0, K_LARGH_PRODOTTO - 1) + "." : item.NomeProdotto.PadRight(K_LARGH_PRODOTTO))}".ToUpper(),
           (byte)ThermalMemoryStream.PrintingStyle.Bold);
        }
        memoryStreamPrinter.SetAlignCenter();
        memoryStreamPrinter.HorizontalLine(K_LARGH_RIGA);

        memoryStreamPrinter.LineFeed(K_LINE_FEEDS);
        memoryStreamPrinter.CutRequest();
        #endregion

        memoryStreamPrinter.Sleep();
        ret = memoryStream.ToArray();
        memoryStream.Close();
      }
      return (ret);
    }
    private void Stampa_Intestazione(string strNomeGiornata, ArchFeste Festa, ArchOrdini Ordine, AnagrCasse Cassa, ThermalMemoryStream memoryStream, bool flagStampaOraOrdine = true)
    {
      memoryStream.SetAlignCenter();
      memoryStream.WriteLine(Festa.Associazione, (byte)ThermalMemoryStream.PrintingStyle.Bold + (byte)ThermalMemoryStream.PrintingStyle.DoubleHeight);
      memoryStream.WriteLine(Festa.Festa, (byte)ThermalMemoryStream.PrintingStyle.Bold + (byte)ThermalMemoryStream.PrintingStyle.DoubleHeight);

      if (!(Cassa is null))
      {
        if (Cassa.IdCassa > 0)
        {
          memoryStream.WriteLine($"Cassa {Cassa.IdCassa} - {strNomeGiornata}", (byte)ThermalMemoryStream.PrintingStyle.Bold);
        }
        else
        {
          memoryStream.WriteLine($"Cumulativo - {strNomeGiornata}", (byte)ThermalMemoryStream.PrintingStyle.Bold);
        }
      }
      else
      {
        memoryStream.WriteLine($"Cumulativo - {strNomeGiornata}", (byte)ThermalMemoryStream.PrintingStyle.Bold);
      }

      if (flagStampaOraOrdine)
      {
        memoryStream.SetAlignCenter();
        memoryStream.WriteLine($"- {Ordine.DataOra:dd/MM/yyyy HH:mm:ss} -".PadLeft(K_LARGH_RIGA), (byte)ThermalMemoryStream.PrintingStyle.Bold + (byte)ThermalMemoryStream.PrintingStyle.Underline);
      }
      memoryStream.SetAlignCenter();
      memoryStream.HorizontalLine(K_LARGH_RIGA);
    }
    private void Stampa_PièDiPagina(ArchFeste Festa, ArchOrdini Ordine, ThermalMemoryStream memoryStream, bool CutRequest = true)
    {
      memoryStream.LineFeed();
      memoryStream.SetAlignCenter();
      if (Festa.Ricevuta_Riga0 != string.Empty)
      {
        memoryStream.WriteLine(Festa.Ricevuta_Riga0, ThermalMemoryStream.PrintingStyle.Bold);
        memoryStream.LineFeed();
      }

      if (Ordine.TipoOrdine == "SERVITO")
      {
        if (Festa.Ricevuta_Riga4 != string.Empty)
        {
          memoryStream.WriteLine(Festa.Ricevuta_Riga1, ThermalMemoryStream.PrintingStyle.Bold);
          memoryStream.WriteLine(Festa.Ricevuta_Riga2, ThermalMemoryStream.PrintingStyle.Bold);
          memoryStream.WriteLine(Festa.Ricevuta_Riga3, ThermalMemoryStream.PrintingStyle.Bold);
          memoryStream.WriteLine(Festa.Ricevuta_Riga4, ThermalMemoryStream.PrintingStyle.Bold);
        }
        else if (Festa.Ricevuta_Riga3 != string.Empty)
        {
          memoryStream.WriteLine(Festa.Ricevuta_Riga1, ThermalMemoryStream.PrintingStyle.Bold);
          memoryStream.WriteLine(Festa.Ricevuta_Riga2, ThermalMemoryStream.PrintingStyle.Bold);
          memoryStream.WriteLine(Festa.Ricevuta_Riga3, ThermalMemoryStream.PrintingStyle.Bold);
        }
        else if (Festa.Ricevuta_Riga2 != string.Empty)
        {
          memoryStream.WriteLine(Festa.Ricevuta_Riga1, ThermalMemoryStream.PrintingStyle.Bold);
          memoryStream.WriteLine(Festa.Ricevuta_Riga2, ThermalMemoryStream.PrintingStyle.Bold);
        }
        else if (Festa.Ricevuta_Riga1 != string.Empty)
        {
          memoryStream.WriteLine(Festa.Ricevuta_Riga1, ThermalMemoryStream.PrintingStyle.Bold);
        }
      }
      memoryStream.LineFeed(K_LINE_FEEDS);
      if (CutRequest)
        memoryStream.CutRequest();
    }
  }
}


