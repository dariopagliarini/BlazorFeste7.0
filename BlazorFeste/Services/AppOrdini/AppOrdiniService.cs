using BlazorFeste.Data.Models;

using System.Collections.Concurrent;

namespace BlazorFeste.Services.AppOrdini
{
  public class AppOrdiniService : IAppOrdiniService
  {
    private readonly ConcurrentQueue<long> _queue = new();
    private string _appListaOrdini = string.Empty;

    public bool HasOrdiniDaEvadereReady { get => !_queue.IsEmpty; }
    public void AddOrdineDaEvadere(long idOrdine) => _queue.Enqueue(idOrdine);
    public long GetOrdiniDaEvadere()
    {
      long _ordineDaEvadere;
      _queue.TryDequeue(out _ordineDaEvadere);
      return _ordineDaEvadere;
    }

    public void UpdateListaOrdini(string appListaOrdini) => _appListaOrdini = appListaOrdini;
    public string GetListaOrdini()
    {
      return _appListaOrdini;
    }
  }
}
