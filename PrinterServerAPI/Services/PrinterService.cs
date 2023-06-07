using BlazorFeste.Data.Models;
using PrinterServerAPI.lib;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO.Ports;
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
      // Get a list of serial port names.
      string[] ports = SerialPort.GetPortNames();

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
        //Log.Information($"Id: {localItem.Ordine.ProgressivoSerata} - Consuma       - Elapsed {s.ElapsedMilliseconds}");
      }
    }
    private void Stampa_RawData(StampaOrdine_RawData stampaOrdine)
    {
      //Serial port init
      using (SerialPort printerPort = new SerialPort(stampaOrdine.Stampante, 38400))
      {
        // Log.Information($"printerPort - {Cassa.Stampante}");
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

        //Printer init
        ThermalPrinter printer = new ThermalPrinter(printerPort, 4, 80, 2);
        printer.WakeUp();
        for (int i = 0; i < stampaOrdine.rawData.Length; i++)
        {
          printerPort.Write(stampaOrdine.rawData, i, 1);
        }
        printer.Sleep();
        printerPort.Close();
        //Log.Information($"Stampa scontrino OK");
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
