using System;

namespace BlazorFeste.Data.Models
{
  public partial class ArchFeste
  {
    public int IdFesta { get; set; }
    public string Festa { get; set; }
    public string Associazione { get; set; }
    public int IdListino { get; set; }
    public DateTime? DataInizio { get; set; }
    public DateTime? DataFine { get; set; }
    public bool Visibile { get; set; }
    public bool FestaAttiva { get; set; }
    public string Ricevuta_Riga0 { get; set; }
    public string Ricevuta_Riga1 { get; set; }
    public string Ricevuta_Riga2 { get; set; }
    public string Ricevuta_Riga3 { get; set; }
    public string Ricevuta_Riga4 { get; set; }
    public bool WebAppAttiva { get; set; }
  }
}
