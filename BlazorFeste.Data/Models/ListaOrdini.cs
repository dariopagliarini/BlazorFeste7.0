namespace BlazorFeste.Data.Models
{
  public class ListaOrdini
  {
    public long IdOrdine { get; set; }
    public int ProgressivoSerata { get; set; }
    public int IdStatoOrdine { get; set; }
    public string Cassa { get; set; }
    public string DataOra { get; set; }
    public string Tavolo { get; set; }
    public string NumeroCoperti { get; set; }
    public string Referente { get; set; }
    public string NoteOrdine { get; set; }
    public ArchOrdiniRighe[] Righe { get; set; }
    public string RigheHTML { get; set; }
    public int? Priorità { get; set; }
    public int[] StatoRighe { get; set; } = new int[4];
    public long AppIdOrdine { get; set; } // IdOrdine da webApp
  }
}
