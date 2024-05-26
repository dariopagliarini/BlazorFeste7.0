using Dapper.Contrib.Extensions;

namespace BlazorFeste.Data.Models
{
  [Table("anagr_menu")]
  public record AnagrMenu
  {
    [Key]
    public int IdMenu { get; set; }
    public string Menu { get; set; }
    public string Icona { get; set; }
  }
}
