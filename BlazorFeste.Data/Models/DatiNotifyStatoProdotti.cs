using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlazorFeste.Data.Models
{
  public record DatiNotifyStatoProdotti
  {
    public int idCassa { get; set; }
    public List<AnagrProdotti> statoProdotti { get; set; }
  }
}
