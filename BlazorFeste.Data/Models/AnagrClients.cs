using Dapper.Contrib.Extensions;

namespace BlazorFeste.Data.Models
{
  [Table("anagr_clients")]
  public record AnagrClients
  {
    [Key]
    public string IndirizzoIP { get; set; }
    public int Livello { get; set; }
  }
}
