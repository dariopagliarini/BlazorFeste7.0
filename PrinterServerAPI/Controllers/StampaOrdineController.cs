using BlazorFeste.Data.Models;
using Microsoft.AspNetCore.Mvc;
using PrinterServerAPI.Services;
using Serilog;

namespace PrinterServerAPI.Controllers
{
  [ApiController]
  [Route("api/[controller]")]
  public class StampaOrdineController : ControllerBase
  {
    private readonly PrinterService _printerService;

    public StampaOrdineController(PrinterService printerService) => _printerService = printerService;

    [HttpPost]
    [Route("rescanSerialPorts")]
    public IActionResult Post([FromBody] bool LogEnabled)
    {
      if (LogEnabled)
        Log.Information($"HTTPPost - rescanSerialPorts -------");

      _printerService.RescanSerialPorts(LogEnabled);
      return (Ok());
    }

    [HttpPost]
    [Route("rawData")]
    public IActionResult Post([FromBody] StampaOrdine_RawData _rawData)
    {
      if (_rawData.LogEnabled)
        Log.Information($"HTTPPost - {_rawData.IPAddress} - {_rawData.DebugText}");

      if (_printerService.Accoda_Ordine(_rawData))
      {
        return (Ok());
      }
      return (NotFound("Stampante non trovata"));
    }
  }
}
