﻿namespace BlazorFeste.Data.Models
{
  public record StatoListe
  {
    public int IdLista { get; set; }
    public string Lista { get; set; }
    public int OrdiniInCoda { get; set; }
    public int OrdiniInCorso { get; set; }
    public int OrdiniEvasi { get; set; }

  }
}
