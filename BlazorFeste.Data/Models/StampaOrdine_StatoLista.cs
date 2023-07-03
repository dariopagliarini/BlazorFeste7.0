using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlazorFeste.Data.Models
{
  public class StampaOrdine_StatoLista
  {
    public int IdProdotto { get; set; }
    public string NomeProdotto { get; set; }
    public double Importo { get; set; }
    public int Quantità { get; set; }
  }
}
