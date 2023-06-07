namespace BlazorFeste.Data.Models
{
  public class TabellaProdotti
  {
    public int IdListino { get; set; }
    public int IdProdotto { get; set; }
    public string NomeProdotto { get; set; }
    public double? PrezzoUnitario { get; set; }
    public bool Stato { get; set; }
    public uint Magazzino { get; set; }
    public uint Consumo { get; set; }
    public string BackColor { get; set; }
    public string ForeColor { get; set; }

  }
}
