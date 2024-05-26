using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlazorFeste.Data.Models
{
  public class AppListaOrdini
  {
    public long id { get; set; }
    public string table { get; set; }
    public int coperti { get; set; }
    public string referente { get; set; }
    public Boolean pos { get; set; }
    public Boolean evaso { get; set; }
    public DateTime dataCloud { get; set; }
    public string confTavolo { get; set; }
    public string note { get; set; }
  }
}
