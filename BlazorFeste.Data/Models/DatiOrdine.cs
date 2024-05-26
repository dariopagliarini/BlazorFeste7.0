using System.Collections.Generic;

namespace BlazorFeste.Data.Models
{
  public record DatiOrdine
  {
    public ArchOrdini ordine { get; set; }
    public List<ArchOrdiniRighe> ordineRighe { get; set; }

  }
}
