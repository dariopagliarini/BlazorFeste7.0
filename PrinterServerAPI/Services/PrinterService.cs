using BlazorFeste.Data.Models;

using RJCP.IO.Ports;

using Serilog;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace PrinterServerAPI.Services
{
  public class PrinterService
  {
    readonly List<RawDataBlockingCollection> rawDataBlockingCollections = new();

    public PrinterService()
    {
      RescanSerialPorts(true);
    }
    public void RescanSerialPorts(bool LogEnabled)
    {
      string[] ports;
      using (SerialPortStream printerPort = new SerialPortStream())
      {
        ports = printerPort.GetPortNames();
      }

      rawDataBlockingCollections.Clear();
      if (ports.Length == 0)
      {
        if (LogEnabled)
          Log.Information($"  Nessuna Porta Seriale disponibile");
      }
      else
      {
        // Display each port name to the console.
        if (LogEnabled)
          Log.Information($"  Elenco Porte Seriali");
        foreach (string port in ports)
        {
          rawDataBlockingCollections.Add(new RawDataBlockingCollection { Stampante = port, StampaInCorso = false, Bc = new() });

          if (LogEnabled)
            Log.Information($"    Porta {port} disponibile");
        }
      }
    }
    public bool Accoda_Ordine(StampaOrdine_RawData stampaOrdine)
    {
      RawDataBlockingCollection rawDataBlockingCollection = rawDataBlockingCollections.Where(w => w.Stampante.Equals(stampaOrdine.Stampante)).FirstOrDefault();
      if (rawDataBlockingCollection != null) // La coda di stampa esiste
      {
        rawDataBlockingCollection.Bc.Add(stampaOrdine);

        if (!rawDataBlockingCollection.StampaInCorso)
        {
          rawDataBlockingCollection.StampaInCorso = true;
          Consuma(rawDataBlockingCollection);
          rawDataBlockingCollection.StampaInCorso = false;
        }
        else
        {
          Log.Information($"  StampaInCorso - {stampaOrdine}");
        }
        return true;
      }
      Log.Information($"Porta {stampaOrdine.Stampante} non disponibile");
      return false;
    }
    private void Consuma(RawDataBlockingCollection rawDataBlockingCollection)
    {
      StampaOrdine_RawData localItem;
      while (rawDataBlockingCollection.Bc.TryTake(out localItem))
      {
        //var s = Stopwatch.StartNew();
        Stampa_RawData(localItem);
        //Log.Information($"Consuma - {localItem.DebugText} - Elapsed {s.ElapsedMilliseconds} ({rawDataBlockingCollection.Bc.Count})");
      }
    }
    private void Stampa_RawData(StampaOrdine_RawData stampaOrdine)
    {
      //Serial port init
      using (SerialPortStream printerPort = new SerialPortStream(stampaOrdine.Stampante, 38400))
      {
        //Log.Information($"printerPort - {Cassa.Stampante}");
        if (printerPort != null)
        {
          // Log.Information("printerPort - Port OK");

          if (printerPort.IsOpen)
          {
            printerPort.Close();
          }
        }
        //Log.Information("Opening port");
        try
        {
          printerPort.Open();
        }
        catch (Exception ex)
        {
          Log.Fatal(ex, $"printerPort.Open - I/O error");
          return;
        }

        try
        {
          printerPort.Write(stampaOrdine.rawData, 0, stampaOrdine.rawData.Length);
          printerPort.Flush();
          //for (int i = 0; i < stampaOrdine.rawData.Length; i++)
          //{
          //  printerPort.Write(stampaOrdine.rawData, i, 1);
          //}
        }
        catch (Exception ex)
        {
          Log.Fatal(ex, $"printerPort.Write - I/O error");
          return;
        }
        printerPort.Close();

        //Log.Information($"Stampa_RawData - Stampa scontrino OK - {stampaOrdine.rawData.Length} bytes");
      }
    }

    public class RawDataBlockingCollection
    {
      public string Stampante { get; set; }
      public bool StampaInCorso { get; set; }
      public BlockingCollection<StampaOrdine_RawData> Bc { get; set; }
    }

  }
}
