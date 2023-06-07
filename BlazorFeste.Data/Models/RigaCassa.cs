using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BlazorFeste.Data.Models
{
  public class RigaCassa
  {
    public int IdProdotto { get; set; }
    public string NomeProdotto { get; set; }
    public double PrezzoUnitario { get; set; }
    public int QuantitàProdotto { get; set; }
  }
}
