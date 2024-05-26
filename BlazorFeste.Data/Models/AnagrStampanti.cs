using Dapper.Contrib.Extensions;

namespace BlazorFeste.Data.Models
{
  [Table("anagr_stampanti")]
  public class AnagrStampanti
  {
    [Key]
    public int IdStampante { get; set; }
    public string SerialPort { get; set; }
    public bool IsRemote { get; set; }
    public string RemoteAddress { get; set; }
    public string Note { get; set; }
  }
}
