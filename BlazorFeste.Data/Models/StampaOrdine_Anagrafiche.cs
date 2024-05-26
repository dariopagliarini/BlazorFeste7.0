using System.Collections.Generic;

namespace BlazorFeste.Data.Models
{
  public class StampaOrdine_Anagrafiche
  {
    public string IPAddress { get; set; }
    public List<AnagrProdotti> anagr_Prodotti { get; set; }
  }
}
