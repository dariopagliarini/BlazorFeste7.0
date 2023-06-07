using System;
using System.Collections.Generic;
using System.Linq;

namespace BlazorFeste.Data.Models
{
  public class StampaOrdine_ScontrinoCucina
  {
    //public string IPAddress { get; set; }
    public string strNomeGiornata { get; set; }
    public ArchFeste Festa { get; set; }
    public AnagrCasse Cassa { get; set; }
    public ArchOrdini Ordine { get; set; }
    public AnagrListe ListaDaStampare { get; set; }
    public List<ArchOrdiniRighe> RigheOrdine { get; set; }

  }
}
