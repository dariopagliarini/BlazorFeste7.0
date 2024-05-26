using System.Collections.Generic;

namespace BlazorFeste.Data.Models
{
  public record DatiNotifyDashboard
  {
    public long elapsed_GetDatabaseData { get; set; }
    public long elapsed_GetDashBoardData { get; set; }
    public ArchFeste Festa { get; set; }
    public List<ArchOrdini> Ordini { get; set; } = new();
    public List<ArchOrdiniRighe> OrdiniRighe { get; set; } = new();
    public List<AnagrCasse> AnagrCasse { get; set; } = new();
    public List<AnagrListe> AnagrListe { get; set; } = new();
    public List<AnagrProdotti> AnagrProdotti { get; set; } = new();
  }
}
