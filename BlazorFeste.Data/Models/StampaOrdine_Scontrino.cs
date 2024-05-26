using System.Collections.Generic;

namespace BlazorFeste.Data.Models
{
  public class StampaOrdine_Scontrino
  {
    //public string IPAddress { get; set; }
    public string strNomeGiornata { get; set; }
    public ArchFeste Festa { get; set; }
    public AnagrCasse Cassa { get; set; }
    public ArchOrdini Ordine { get; set; }
    public List<ArchOrdiniRighe> RigheOrdine { get; set; }
    public List<AnagrListe> ListeDaStampare { get; set; }
    public List<ArchOrdiniRighe> QueueTicketDaStampare { get; set; }
    public bool ScontrinoMuto { get; set; } = false;
  }
}
