using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlazorFeste.Data.Models
{
  public record DatiOrdine
  {
    public ArchOrdini ordine { get; set; }
    public List<ArchOrdiniRighe> ordineRighe { get; set; }

  }
}
