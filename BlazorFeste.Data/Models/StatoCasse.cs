namespace BlazorFeste.Data.Models
{
  public record StatoCasse
  {
    public int IdCassa { get; set; }
    public double Importo { get; set; }
    public int QuantitàProdotto { get; set; }

    public double ImportoContanti { get; set; }
    public double ImportoPOS { get; set; }

  }

  public record DatiCassa
  {
    public int IdCassa { get; set; }
    public bool PagamentoConPOS { get; set; }
    public double Importo { get; set; }

  }
}
