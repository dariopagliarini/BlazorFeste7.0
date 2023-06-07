using System;
using System.Collections.Generic;
using Dapper.Contrib.Extensions;

namespace BlazorFeste.Data.Models
{
  [Table("arch_ordini")]
  public record ArchOrdini
  {
    [Key]
    public long IdOrdine { get; set; }
    public string Cassa { get; set; }
    public DateTime DataOra { get; set; }
    public string TipoOrdine { get; set; }
    public string Tavolo { get; set; }
    public string NumeroCoperti { get; set; }
    public int IdStatoOrdine { get; set; }
    public DateTime Timestamp { get; set; }
    public DateTime DataAssegnazione { get; set; }
    public string Referente { get; set; }
    public string NoteOrdine { get; set; }
    public int ProgressivoSerata { get; set; }
    public int IdFesta { get; set; }

    [Computed]
    public int IdCassa { get => int.Parse(Cassa); }
    [Computed]
    public string strDataAssegnazione { 
      get {
        return $"{DataAssegnazione.ToString("ddd dd/MM").ToUpper()} - {(DataAssegnazione.Hour == 12 ? "PRANZO" : "CENA")}";
      }
    }
    [Computed]
    public List<ArchOrdiniRighe> righe { get; set; }
  }
}
