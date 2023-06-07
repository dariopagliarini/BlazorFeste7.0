using System.Collections.Generic;

namespace BlazorFeste.Data.Models
{
  public record StatoOrdini
  {
    public int IdProdotto { get; set; }
    public string NomeProdotto { get; set; }
    public double Importo { get; set; }
    public int Quantità { get; set; }
    public List<StatoCasse> statoCassa { get; set; }
  }
}
