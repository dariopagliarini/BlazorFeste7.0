using System;
using Dapper.Contrib.Extensions;

namespace BlazorFeste.Data.Models
{
  [Table("arch_ordini_righe")]
  public class ArchOrdiniRighe
  {
    [Key]
    public long IdOrdine { get; set; }
    [Key]
    public int IdRiga { get; set; }
    public int IdCategoria { get; set; }
    public string Categoria { get; set; }
    public int IdProdotto { get; set; }
    public string NomeProdotto { get; set; }
    public int IdStatoRiga { get; set; }
    public int QuantitàProdotto { get; set; }
    public int QuantitàEvasa { get; set; }
    public double Importo { get; set; }
    public DateTime DataOra_RigaPresaInCarico { get; set; }
    public DateTime DataOra_RigaEvasa { get; set; }
    public uint QueueTicket { get; set; }
  }
}
