using Dapper.Contrib.Extensions;

using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace BlazorFeste.Data.Models
{
  [Table("anagr_casse")]
  public record AnagrCasse
  {
    [Key]
    public int IdPrimaryKey { get; set; }
    public int IdListino { get; set; }
    public int IdCassa { get; set; }
    public string Cassa { get; set; }
    public bool? Abilitata { get; set; }
    public bool? Visibile { get; set; }
    public string PortName { get; set; }
    public bool? IsRemote { get; set; }
    public string RemoteAddress { get; set; }
    public string Prodotti { get; set; }
    public string BackColor { get; set; }
    public string ForeColor { get; set; }
    public bool? SoloBanco { get; set; }
    public bool? ScontrinoAbilitato { get; set; }
    public bool? ScontrinoMuto { get; set; }

    [Computed]
    public long idUltimoOrdine { get; set; }
    [Computed]
    public long OrdiniDellaCassa { get; set; }
    [Computed]
    public IEnumerable<int> prodottiVisibili
    {
      get
      {
        foreach (string s in Prodotti.Split(','))
        {
          // try and get the number
          int num;
          if (int.TryParse(s, out num))
          {
            yield return num;
            continue; // skip the rest
          }

          // otherwise we might have a range
          // split on the range delimiter
          string[] subs = s.Split('-');
          int start, end;

          // now see if we can parse a start and end
          if (subs.Length > 1 &&
              int.TryParse(subs[0], out start) &&
              int.TryParse(subs[1], out end) &&
              end >= start)
          {
            // create a range between the two values
            int rangeLength = end - start + 1;
            foreach (int i in Enumerable.Range(start, rangeLength))
            {
              yield return i;
            }
          }
        }
      }
    }
  }
}
