using System.Collections.Generic;

namespace BlazorFeste.Data.Models
{
  public record DatiNotifyStatoProdotti
  {
    public int idCassa { get; set; }
    public List<AnagrProdotti> statoProdotti { get; set; }
  }
}
