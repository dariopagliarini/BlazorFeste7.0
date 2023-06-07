using System.Text.Json.Serialization;

namespace BlazorFeste.Data.Models
{
  public record StatoCasse
  {
    public int IdCassa { get; set; }
    public double Importo { get; set; }
    public int QuantitàProdotto { get; set; }
  }
}
