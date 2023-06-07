using BlazorFeste.Data.Models;
using BlazorFeste.DataAccess;

using Serilog;

using System.Text.RegularExpressions;

namespace BlazorFeste.Services
{
  public class DatabaseTimerService : BackgroundService, IDisposable
  {
    private CancellationToken cancellationToken;
    private const int _runTime = 1000;

    private readonly UserInterfaceService _UserInterfaceService;
    private readonly FesteDataAccess _FesteDataAccess;

    public DatabaseTimerService(UserInterfaceService userInterfaceService, FesteDataAccess festeDataAccess)
    {
      _UserInterfaceService = userInterfaceService;
      _FesteDataAccess = festeDataAccess;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
      int memoOra = -1;

      cancellationToken = stoppingToken;
      while (!cancellationToken.IsCancellationRequested)
      {
        _UserInterfaceService.elapsed_GetDatabaseData = await GetDatabaseData();

        // Aggiornamento dei dati per i clients
        _UserInterfaceService.OnUpdateListe($"{_UserInterfaceService.elapsed_GetDatabaseData}");

        // Ogni ora devo fare qualcosa ???  - TODO
        if (DateTime.UtcNow.Hour != memoOra)
        {
          memoOra = DateTime.UtcNow.Hour;
        }

        await Task.Delay(TimeSpan.FromMilliseconds(Math.Max(0, _runTime - _UserInterfaceService.elapsed_GetDatabaseData)), stoppingToken);
      }
    }
    public override void Dispose()
    {
      CancellationTokenSource source = new();
      cancellationToken = source.Token;
      source.Cancel();
    }

    private async Task<long> GetDatabaseData()
    {
      var watch = System.Diagnostics.Stopwatch.StartNew();
      try
      {
        // Verifico se è cambiata la Data di assegnazione
        if (_UserInterfaceService.GetCurrentDataAssegnazione() != _UserInterfaceService.DtFestaInCorso)
        {
          // Recupero la Data della giornata in corso 
          _UserInterfaceService.DtFestaInCorso = _UserInterfaceService.GetCurrentDataAssegnazione();

          // Recupero le informazioni della Festa in corso
          _UserInterfaceService.ArchFesta = (await _FesteDataAccess.GetArchFesteAsync(_UserInterfaceService.DtFestaInCorso)).FirstOrDefault();

          // Recupero l'anagrafica delle Liste legate al listino della festa in corso
          _UserInterfaceService.AnagrListe = (await _FesteDataAccess.GetGenericQuery<AnagrListe>("SELECT * FROM anagr_liste WHERE Abilitata <> 0 AND IdListino = @IdListino ORDER BY IdLista ",
            new { IdListino = _UserInterfaceService.ArchFesta.IdListino })).ToList();


          // Recupero l'anagrafica dei Prodotti legati al listino della festa in corso
#if THREADSAFE
          List<AnagrProdotti> _anagrProdotti = (await _FesteDataAccess.GetGenericQuery<AnagrProdotti>("SELECT *, COUNT(IdProdotto) OVER (PARTITION BY IdLista ORDER BY IdProdotto) AS Ordine FROM anagr_prodotti WHERE IdListino = @IdListino ORDER BY IdProdotto ",
            new { IdListino = _UserInterfaceService.ArchFesta.IdListino })).ToList();

          _UserInterfaceService.AnagrProdotti.Clear();
          foreach (var prodotto in _anagrProdotti)
          {
            _UserInterfaceService.AnagrProdotti.TryAdd(prodotto.IdProdotto, prodotto);
          }

          // Recupero le informazioni in caso di mancata configurazione
          foreach (var item in _UserInterfaceService.AnagrProdotti.Values)
          {
            if (String.IsNullOrEmpty(item.BackColor))
            {
              item.BackColor = _UserInterfaceService.AnagrListe.Where(w => w.IdLista == item.IdLista).FirstOrDefault().BackColor;
            }
            if (String.IsNullOrEmpty(item.ForeColor))
            {
              item.ForeColor = _UserInterfaceService.AnagrListe.Where(w => w.IdLista == item.IdLista).FirstOrDefault().ForeColor;
            }
          }
#else
          _UserInterfaceService.AnagrProdotti = (await _FesteDataAccess.GetGenericQuery<AnagrProdotti>("SELECT *, COUNT(IdProdotto) OVER (PARTITION BY IdLista ORDER BY IdProdotto) AS Ordine FROM anagr_prodotti WHERE IdListino = @IdListino ORDER BY IdProdotto ",
            new { IdListino = _UserInterfaceService.ArchFesta.IdListino })).ToList();

          // Recupero le informazioni in caso di mancata configurazione
          foreach (var item in _UserInterfaceService.AnagrProdotti)
          {
            if (String.IsNullOrEmpty(item.BackColor))
            {
              item.BackColor = _UserInterfaceService.AnagrListe.Where(w => w.IdLista == item.IdLista).FirstOrDefault().BackColor;
            }
            if (String.IsNullOrEmpty(item.ForeColor))
            {
              item.ForeColor = _UserInterfaceService.AnagrListe.Where(w => w.IdLista == item.IdLista).FirstOrDefault().ForeColor;
            }
          }

#endif
          _UserInterfaceService.AnagrCasse = (await _FesteDataAccess.GetGenericQuery<AnagrCasse>("SELECT * FROM anagr_casse WHERE Abilitata <> 0 AND IdListino = @IdListino ORDER BY IdCassa ",
            new { IdListino = _UserInterfaceService.ArchFesta.IdListino })).ToList();

          _UserInterfaceService.AnagrStampanti = (await _FesteDataAccess.GetFromMySQLAsync<AnagrStampanti>(cancellationToken)).ToList();
        }

#if THREADSAFE
        List<ArchOrdini> _qryOrdini = (await _FesteDataAccess.GetGenericQuery<ArchOrdini>("SELECT * FROM arch_ordini WHERE DataAssegnazione = @DataAssegnazione", new { DataAssegnazione = _UserInterfaceService.DtFestaInCorso })).ToList();
        List<ArchOrdiniRighe> _qryOrdiniRighe = (await _FesteDataAccess.GetGenericQuery<ArchOrdiniRighe>("SELECT r.* FROM arch_ordini o JOIN arch_ordini_righe r ON o.IdOrdine = r.IdOrdine WHERE o.DataAssegnazione = @DataAssegnazione", new { DataAssegnazione = _UserInterfaceService.DtFestaInCorso })).ToList();

        _UserInterfaceService.QryOrdini.Clear();
        foreach (var ordine in _qryOrdini)
        {
          _UserInterfaceService.QryOrdini.TryAdd(ordine.IdOrdine, ordine);
        }

        _UserInterfaceService.QryOrdiniRighe.Clear();
        foreach (var riga in _qryOrdiniRighe)
        {
          _UserInterfaceService.QryOrdiniRighe.TryAdd(new Tuple<long, int>(riga.IdOrdine, riga.IdRiga), riga);
        }

        // Elaborazione degli ordini appena nati
        List<ArchOrdini> qryOrdiniAppenaNati = _UserInterfaceService.QryOrdini.Where(w => w.Value.IdStatoOrdine == 0).OrderBy(o => o.Key).Select(s => s.Value).ToList();

        // Se esiste almeno un ordine "appena nato"
        foreach (var Ordine in qryOrdiniAppenaNati)
        {
          // Se l'ordine non ha righe lo escludo da ulteriori logiche (oppure lo elimino)
          if (!_UserInterfaceService.QryOrdiniRighe.Where(w => w.Key.Item1 == Ordine.IdOrdine).Any())
          {
            await _FesteDataAccess.DeleteArchOrdiniAsync(Ordine); // Elimino l'Ordine dal Database - // Ordine.IdStatoOrdine = 4;

            ArchOrdini _Ordine;
            _UserInterfaceService.QryOrdini.TryRemove(Ordine.IdOrdine, out _Ordine);  // Elimino l'Ordine dalla struttura di memoria
          }
          else
          {
            var Cassa = _UserInterfaceService.AnagrCasse.Where(w => w.IdCassa == Ordine.IdCassa).FirstOrDefault();

            if (Cassa.SoloBanco.Value)
            {
              // Se l'ordine arriva da una cassa "SoloBanco" devo evadere l'ordine
              await EvadiOrdine(Ordine.IdOrdine);
            }
            else
            {
              // Devo evadere tutte le liste che non sono gestite - TODO
              //await EvadiOrdine(Ordine.IdOrdine, _UserInterfaceService.AnagrProdotti, _UserInterfaceService.AnagrListe.Where(w => w.Visibile == false).ToList());

              // Verifico se esistono righe dell'ordine con IdListaPadre > 0 (cioè con delle code a priorità maggiore ancora da smaltire)
              Ordine.IdStatoOrdine = GetStatoOrdine(Ordine);

              Ordine.Tavolo = Ordine.TipoOrdine.CompareTo("SERVITO") == 0 ? string.Format("{0} {1}", Ordine.Tavolo, Regex.Replace(Ordine.NumeroCoperti, @"[^a-zA-Z]+", string.Empty).ToUpper()).Trim() : Ordine.TipoOrdine;
              Ordine.NumeroCoperti = Regex.Replace(Ordine.NumeroCoperti, @"[^0-9]+", string.Empty).Trim();
              Ordine.DataOra = DateTime.Now;

              // Aggiorno il record dell'Ordine nel Database
              await _FesteDataAccess.UpdateArchOrdiniAsync(Ordine);

            }
            // Se arrivo qui significa che :
            //    Devo notificare a chi lo desidera i dati del nuovo ordine
            _UserInterfaceService.OnNotifyStatoOrdine(Ordine.IdOrdine);
          }
        }
#else
        _UserInterfaceService.QryOrdini = (await _FesteDataAccess.GetGenericQuery<ArchOrdini>("SELECT * FROM arch_ordini WHERE DataAssegnazione = @DataAssegnazione", new { DataAssegnazione = _UserInterfaceService.DtFestaInCorso })).ToList();
        _UserInterfaceService.QryOrdiniRighe = (await _FesteDataAccess.GetGenericQuery<ArchOrdiniRighe>("SELECT r.* FROM arch_ordini o JOIN arch_ordini_righe r ON o.IdOrdine = r.IdOrdine WHERE o.DataAssegnazione = @DataAssegnazione", new { DataAssegnazione = _UserInterfaceService.DtFestaInCorso })).ToList();

        // Elaborazione degli ordini appena nati
        List<ArchOrdini> qryOrdiniAppenaNati = _UserInterfaceService.QryOrdini.Where(w => w.IdStatoOrdine == 0).OrderBy(o => o.IdOrdine).ToList();

        // Se esiste almeno un ordine "appena nato"
        foreach (var Ordine in qryOrdiniAppenaNati)
        {
          // Se l'ordine non ha righe lo escludo da ulteriori logiche (oppure lo elimino)
          if (!_UserInterfaceService.QryOrdiniRighe.Where(w => w.IdOrdine == Ordine.IdOrdine).Any())
          {
            await _FesteDataAccess.DeleteArchOrdiniAsync(Ordine); // Elimino l'Ordine dal Database - // Ordine.IdStatoOrdine = 4;
            _UserInterfaceService.QryOrdini.Remove(Ordine);  // Elimino l'Ordine dalla struttura di memoria
          }
          else
          {
            var Cassa = _UserInterfaceService.AnagrCasse.Where(w => w.IdCassa == Ordine.IdCassa).FirstOrDefault();

            if (Cassa.SoloBanco.Value)
            {
              // Se l'ordine arriva da una cassa "SoloBanco" devo evadere l'ordine
              await EvadiOrdine(Ordine.IdOrdine);
            }
            else
            {
              // Devo evadere tutte leliste che non sono gestite - TODO
              //await EvadiOrdine(Ordine.IdOrdine, _UserInterfaceService.AnagrProdotti, _UserInterfaceService.AnagrListe.Where(w => w.Visibile == false).ToList());

              // Verifico se esistono righe dell'ordine con IdListaPadre > 0 (cioè con delle code a priorità maggiore ancora da smaltire)
              Ordine.IdStatoOrdine = GetStatoOrdine(Ordine);

              Ordine.Tavolo = Ordine.TipoOrdine.CompareTo("SERVITO") == 0 ? string.Format("{0} {1}", Ordine.Tavolo, Regex.Replace(Ordine.NumeroCoperti, @"[^a-zA-Z]+", string.Empty).ToUpper()).Trim() : Ordine.TipoOrdine;
              Ordine.NumeroCoperti = Regex.Replace(Ordine.NumeroCoperti, @"[^0-9]+", string.Empty).Trim();
              Ordine.DataOra = DateTime.Now;

              // Aggiorno il record dell'Ordine nel Database
              await _FesteDataAccess.UpdateArchOrdiniAsync(Ordine);

            }
            // Se arrivo qui significa che :
            //    Devo notificare a chi lo desidera i dati del nuovo ordine
            _UserInterfaceService.OnNotifyStatoOrdine(Ordine.IdOrdine);
          }
        }
#endif

      }
      catch (Exception ex)
      {
        Log.Error($"DatabaseTimerService - GetDatabaseData - {ex.Message}");
      }
      watch.Stop();
      return (watch.ElapsedMilliseconds);
    }

    public async Task EvadiOrdine(long IdOrdine)
    {
#if THREADSAFE
      var Ordine = _UserInterfaceService.QryOrdini.Where(w => w.Key == IdOrdine).Select(s => s.Value).FirstOrDefault();
      var OrdinePre = Ordine;
#else
      var Ordine = _UserInterfaceService.QryOrdini.Where(w => w.IdOrdine == IdOrdine).FirstOrDefault();
#endif
      Ordine.IdStatoOrdine = 2;
      Ordine.DataOra = DateTime.Now;

      // Aggiorno il record dell'Ordine nel Database
      await _FesteDataAccess.UpdateArchOrdiniAsync(Ordine);

#if THREADSAFE
      // Tutte le righe dell'ordine vengono messe nello stato 3
      foreach (var item in _UserInterfaceService.QryOrdiniRighe.Where(w => w.Key.Item1 == IdOrdine))
      {
        item.Value.IdStatoRiga = 3;
        item.Value.QuantitàEvasa = item.Value.QuantitàProdotto;

        await _FesteDataAccess.UpdateArchOrdiniRigheAsync(item.Value);

        AnagrProdotti _anagrProdotto = _UserInterfaceService.AnagrProdotti.Values.Where(w => w.IdProdotto == item.Value.IdProdotto).FirstOrDefault();
        _anagrProdotto.Evaso += Convert.ToUInt32(item.Value.QuantitàEvasa);
        if (_anagrProdotto.IdProdotto != _anagrProdotto.EvadiSuIdProdotto)
        {
          // Devo aggiornare anche il prodotto dove tengo il conteggio cumulativo
          AnagrProdotti _anagrProdottoSuCuiEvadere = _UserInterfaceService.AnagrProdotti.Values.Where(w => w.IdProdotto == _anagrProdotto.EvadiSuIdProdotto).FirstOrDefault();
          _anagrProdottoSuCuiEvadere.EvasoCumulativo += Convert.ToUInt32(item.Value.QuantitàEvasa);
          await _FesteDataAccess.UpdateAnagrProdottiAsync(_anagrProdottoSuCuiEvadere);
        }
        else
        {
          _anagrProdotto.EvasoCumulativo += Convert.ToUInt32(item.Value.QuantitàEvasa);
        }
        await _FesteDataAccess.UpdateAnagrProdottiAsync(_anagrProdotto);
      }
#else
      // Tutte le righe dell'ordine vengono messe nello stato 3
      foreach (var item in _UserInterfaceService.QryOrdiniRighe.Where(w => w.IdOrdine == IdOrdine))
      {
        item.IdStatoRiga = 3;

        await _FesteDataAccess.UpdateArchOrdiniRigheAsync(item);

        AnagrProdotti _anagrProdotto = _UserInterfaceService.AnagrProdotti.Where(w => w.IdProdotto == item.IdProdotto).FirstOrDefault();
        _anagrProdotto.Evaso += Convert.ToUInt32(item.QuantitàEvasa);
        if (_anagrProdotto.IdProdotto != _anagrProdotto.EvadiSuIdProdotto)
        {
          // Devo aggiornare anche il prodotto dove tengo il conteggio cumulativo
          AnagrProdotti _anagrProdottoSuCuiEvadere = _UserInterfaceService.AnagrProdotti.Where(w => w.IdProdotto == _anagrProdotto.EvadiSuIdProdotto).FirstOrDefault();
          _anagrProdottoSuCuiEvadere.EvasoCumulativo += Convert.ToUInt32(item.QuantitàEvasa);
          await _FesteDataAccess.UpdateAnagrProdottiAsync(_anagrProdottoSuCuiEvadere);
        }
        else
        {
          _anagrProdotto.EvasoCumulativo += Convert.ToUInt32(item.QuantitàEvasa);
        }
        await _FesteDataAccess.UpdateAnagrProdottiAsync(_anagrProdotto);
      }
#endif
    }
    private int GetStatoOrdine(ArchOrdini archOrdine)
    {
      int Result = 2;

#if THREADSAFE
      var ArchOrdiniRighe = _UserInterfaceService.QryOrdiniRighe.Where(w => w.Key.Item1 == archOrdine.IdOrdine).ToList();

      // Verifico se esistono righe dell'ordine con IdListaPadre > 0 (cioè con delle code a priorità maggiore ancora da smaltire)
      var RigheConListaPadre = from r in ArchOrdiniRighe
                               join p in _UserInterfaceService.AnagrProdotti.Values on r.Value.IdProdotto equals p.IdProdotto
                               join l in _UserInterfaceService.AnagrListe.Where(w => w.IdListaPadre > 0) on p.IdLista equals l.IdLista
                               select new { r, p, l };

      foreach (var Riga in RigheConListaPadre)
      {
        // Verifico se esistono ancora prodotti da smaltire nella lista padre
        var RigheListaPadre = from r in ArchOrdiniRighe.Where(w => w.Value.IdStatoRiga < 3)
                              join p in _UserInterfaceService.AnagrProdotti.Values on r.Value.IdProdotto equals p.IdProdotto
                              join l in _UserInterfaceService.AnagrListe.Where(w => w.IdLista == Riga.l.IdListaPadre) on p.IdLista equals l.IdLista
                              select new { r, p, l };
        // Me ne basta una per decidere che lo stato dell'ordine é = 1
        if (RigheListaPadre.Any())
        {
          Result = 1; // Le liste con priorità = 2 devono aspettare
          break;
        }
      }
#else
      var ArchOrdiniRighe = _UserInterfaceService.QryOrdiniRighe.Where(w => w.IdOrdine == archOrdine.IdOrdine).ToList();

      // Verifico se esistono righe dell'ordine con IdListaPadre > 0 (cioè con delle code a priorità maggiore ancora da smaltire)
      var RigheConListaPadre = from r in ArchOrdiniRighe
                                join p in _UserInterfaceService.AnagrProdotti on r.IdProdotto equals p.IdProdotto
                                join l in _UserInterfaceService.AnagrListe.Where(w => w.IdListaPadre > 0) on p.IdLista equals l.IdLista
                                select new { r, p, l };

      foreach (var Riga in RigheConListaPadre)
      {
        // Verifico se esistono ancora prodotti da smaltire nella lista padre
        var RigheListaPadre = from r in ArchOrdiniRighe.Where(w => w.IdStatoRiga < 3)
                              join p in _UserInterfaceService.AnagrProdotti on r.IdProdotto equals p.IdProdotto
                              join l in _UserInterfaceService.AnagrListe.Where(w => w.IdLista == Riga.l.IdListaPadre) on p.IdLista equals l.IdLista
                              select new { r, p, l };
        // Me ne basta una per decidere che lo stato dell'ordine é = 1
        if (RigheListaPadre.Any())
        {
          Result = 1; // Le liste con priorità = 2 devono aspettare
          break;
        }
      }
#endif
      return Result;
    }
  }
}
