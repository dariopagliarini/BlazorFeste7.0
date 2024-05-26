using BlazorFeste.Data.Models;

namespace BlazorFeste.Services.AppOrdini
{
  public interface IAppOrdiniService
  {
    bool HasOrdiniDaEvadereReady {  get; }

    void AddOrdineDaEvadere(long idOrdine);
    public long GetOrdiniDaEvadere();

    public void UpdateListaOrdini(string appListaOrdini);
    public string GetListaOrdini();
  }
}
