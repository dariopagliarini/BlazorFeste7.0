using Dapper.Contrib.Extensions;

namespace BlazorFeste.Data.Models
{
  [Table("anagr_liste")] 
  public class AnagrListe
  {
    [Key]
    public int IdPrimaryKey { get; set; }
    public int IdListino { get; set; }
    public int IdLista { get; set; }
    public bool? Abilitata { get; set; }
    public bool? Visibile { get; set; }
    public string Lista { get; set; }
    public int IdListaPadre { get; set; }
    public int? Priorità { get; set; }
    public bool? IoSonoListaPadre { get; set; }
    public string BackColor { get; set; }
    public string ForeColor { get; set; }
    public bool? Tavolo_StampaScontrino { get; set; }
    public bool? Banco_StampaScontrino { get; set; }
    public bool? Cucina_StampaScontrino { get; set; }
    public int? Cucina_NumeroScontrini { get; set; }
    public uint IdStampante { get; set; }
    public bool? StampaNoteOrdine { get; set; }
  }
}
