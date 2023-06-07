using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlazorFeste.Data.Models
{
  public class StampaOrdine_ConsumiGiornata
  {
    public string IPAddress { get; set; }
    public string strNomeGiornata { get; set; }
    public bool flagCumulativo { get; set; }
    public ArchFeste Festa { get; set; }
    public AnagrCasse Cassa { get; set; }
    public List<StampaOrdine_StatoCassa> statoCassa { get; set; }
  }
}
