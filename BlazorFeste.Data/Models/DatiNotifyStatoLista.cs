using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlazorFeste.Data.Models
{
  public record DatiNotifyStatoLista
  {
    public int IdLista { get; set; }
    public string ClientIpAddress { get; set; }
  }
}
