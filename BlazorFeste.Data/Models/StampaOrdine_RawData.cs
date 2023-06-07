using System;
using System.Linq;

namespace BlazorFeste.Data.Models
{
  public class StampaOrdine_RawData
  {
    public string IPAddress { get; set; }
    public string Stampante { get; set; }
    public string DebugText { get; set; }
    public bool LogEnabled { get; set; } = false;
    public byte[] rawData { get; set; }
  }
}
