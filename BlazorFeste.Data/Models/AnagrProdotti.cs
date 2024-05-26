using Dapper.Contrib.Extensions;

namespace BlazorFeste.Data.Models
{
  [Table("anagr_prodotti")]
  public record AnagrProdotti
  {
    [Key]
    public int IdListino { get; set; }
    [Key]
    public int IdProdotto { get; set; }
    public string NomeProdotto { get; set; }
    public double? PrezzoUnitario { get; set; }
    public int IdLista { get; set; }
    public bool Stato { get; set; }
    public uint Magazzino { get; set; }
    public uint Consumo { get; set; }
    public uint Evaso { get; set; }
    public uint ConsumoCumulativo { get; set; }
    public uint EvasoCumulativo { get; set; }
    public int EvadiSuIdProdotto { get; set; }

    [Write(false)]
    public string BackColor { get; set; }
    [Write(false)]
    public string ForeColor { get; set; }
    public bool PrintQueueTicket { get; set; }
    public bool ViewLableDaEvadere { get; set; }
    public int IdMenu { get; set; }

    [Write(false)]
    [Computed]
    public int Ordine { get; set; }

  }
}

