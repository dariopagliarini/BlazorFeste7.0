using Dapper.Contrib.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
