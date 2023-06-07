using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlazorFeste.Data.Models
{
  public class StampaOrdine_Anagrafiche
  {
    public string IPAddress { get; set; }
    public List<AnagrProdotti> anagr_Prodotti { get; set; }
  }
}
