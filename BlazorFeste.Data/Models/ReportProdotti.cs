﻿using System;
using System.Collections.Generic;

namespace BlazorFeste.Data.Models
{
  public class ReportProdotti_Casse
  {
    public AnagrCasse Cassa { get; set; }
    public double Importo { get; set; }
    public Int32 Quantità { get; set; }
  }
  public class ReportProdotti
  {
    public double Importo { get; set; }
    public Int32 Quantità { get; set; }

    public AnagrProdotti Prodotto { get; set; }
    public List<ReportProdotti_Casse> Casse { get; set; }
  }
}
