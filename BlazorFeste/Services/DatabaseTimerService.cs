using BlazorFeste.Constants;
using BlazorFeste.Data.Models;
using BlazorFeste.DataAccess;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Serilog;

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
        _UserInterfaceService.OnNotifyUpdateListe($"{_UserInterfaceService.elapsed_GetDatabaseData} - {_UserInterfaceService.updatesQryOrdini}/{_UserInterfaceService.updatesQryOrdiniRighe}");

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
          _UserInterfaceService.AnagrCasse = (await _FesteDataAccess.GetGenericQuery<AnagrCasse>("SELECT * FROM anagr_casse WHERE Abilitata <> 0 AND IdListino = @IdListino ORDER BY IdCassa ",
            new { IdListino = _UserInterfaceService.ArchFesta.IdListino })).ToList();

          _UserInterfaceService.AnagrStampanti = (await _FesteDataAccess.GetFromMySQLAsync<AnagrStampanti>(cancellationToken)).ToList();

          List<ArchOrdini> _qryOrdiniStartup = (await _FesteDataAccess.GetGenericQuery<ArchOrdini>("SELECT * FROM arch_ordini WHERE DataAssegnazione = @DataAssegnazione", new { DataAssegnazione = _UserInterfaceService.DtFestaInCorso })).ToList();
          List<ArchOrdiniRighe> _qryOrdiniRigheStartup = (await _FesteDataAccess.GetGenericQuery<ArchOrdiniRighe>("SELECT r.* FROM arch_ordini o JOIN arch_ordini_righe r ON o.IdOrdine = r.IdOrdine WHERE o.DataAssegnazione = @DataAssegnazione", new { DataAssegnazione = _UserInterfaceService.DtFestaInCorso })).ToList();

          _UserInterfaceService.QryOrdini.Clear();
          foreach (var ordine in _qryOrdiniStartup)
          {
            _UserInterfaceService.QryOrdini.TryAdd(ordine.IdOrdine, ordine);
          }
          _UserInterfaceService.QryOrdiniRighe.Clear();
          foreach (var riga in _qryOrdiniRigheStartup)
          {
            _UserInterfaceService.QryOrdiniRighe.TryAdd(new Tuple<long, int>(riga.IdOrdine, riga.IdRiga), riga);
          }
        }

        List<ArchOrdini> _qryOrdini = (await _FesteDataAccess.GetGenericQuery<ArchOrdini>("SELECT * FROM arch_ordini WHERE DataAssegnazione = @DataAssegnazione", new { DataAssegnazione = _UserInterfaceService.DtFestaInCorso })).ToList();
        List<ArchOrdiniRighe> _qryOrdiniRighe = (await _FesteDataAccess.GetGenericQuery<ArchOrdiniRighe>("SELECT r.* FROM arch_ordini o JOIN arch_ordini_righe r ON o.IdOrdine = r.IdOrdine WHERE o.DataAssegnazione = @DataAssegnazione", new { DataAssegnazione = _UserInterfaceService.DtFestaInCorso })).ToList();

        // Se ho almeno un ordine fatto nella Data di assegnazione
        if (_qryOrdini.Count > 0)
        {
          // Recupero tutti gli ordini che hanno almeno una riga non evasa per gestire l'aggiornamento della variabile globale
          var ordiniNonEvasi = (from r in _qryOrdiniRighe.Where(w => w.IdStatoRiga < (int)K_STATO_RIGA.OrdineEvaso)
                                select r.IdOrdine).Distinct().ToList();

          foreach (var IdOrdine in ordiniNonEvasi)
          {
            var ordine = _qryOrdini.Where(w => w.IdOrdine == IdOrdine).FirstOrDefault();
            if (_UserInterfaceService.QryOrdini.TryGetValue(IdOrdine, out ArchOrdini retrievedValue))
            {
              // If something changes - Make a copy of the data. 
              if (!JToken.DeepEquals(JsonConvert.SerializeObject(ordine), JsonConvert.SerializeObject(retrievedValue)))
              {
                _UserInterfaceService.updatesQryOrdini++;
                // Replace the old value with the new value.
                if (!_UserInterfaceService.QryOrdini.TryUpdate(IdOrdine, ordine, retrievedValue))
                {
                  // The data was not updated. Log error, throw exception, etc.
                  Log.Error($"DatabaseTimerService - GetDatabaseData - Sincronizzazione QryOrdini - {IdOrdine} non aggiornato");
                }
              }
            }
            else
            {
              if (!_UserInterfaceService.QryOrdini.TryAdd(IdOrdine, ordine))
              {
                Log.Error($"DatabaseTimerService - GetDatabaseData - Sincronizzazione QryOrdini - {IdOrdine} non inserito");
              }
            }
          }

          // Recupero tutte le righe non evase per gestire l'aggiornamento della variabile globale
          foreach (var riga in _qryOrdiniRighe.Where(w => w.IdStatoRiga < (int)K_STATO_RIGA.OrdineEvaso))
          {
            if (_UserInterfaceService.QryOrdiniRighe.TryGetValue(new Tuple<long, int>(riga.IdOrdine, riga.IdRiga), out ArchOrdiniRighe retrievedValue))
            {
              // If something changes - Make a copy of the data. 
              if (!JToken.DeepEquals(JsonConvert.SerializeObject(riga), JsonConvert.SerializeObject(retrievedValue)))
              {
                _UserInterfaceService.updatesQryOrdiniRighe++;
                // Replace the old value with the new value.
                if (!_UserInterfaceService.QryOrdiniRighe.TryUpdate(new Tuple<long, int>(riga.IdOrdine, riga.IdRiga), riga, retrievedValue))
                {
                  // The data was not updated. Log error, throw exception, etc.
                  Log.Error($"DatabaseTimerService - GetDatabaseData - Sincronizzazione QryOrdiniRighe - {riga.IdOrdine}/{riga.IdRiga} non aggiornata");
                }
              }
            }
            else
            {
              if (!_UserInterfaceService.QryOrdiniRighe.TryAdd(new Tuple<long, int>(riga.IdOrdine, riga.IdRiga), riga))
              {
                Log.Error($"DatabaseTimerService - GetDatabaseData - Sincronizzazione QryOrdiniRighe - {riga.IdOrdine}/{riga.IdRiga}  non inserita");
              }
            }
          }

          /*
                    foreach (var ordine in _qryOrdini)
                    {
                      if (_UserInterfaceService.QryOrdini.TryGetValue(ordine.IdOrdine, out ArchOrdini retrievedValue))
                      {
                        // If something changes - Make a copy of the data. 
                        if (!JToken.DeepEquals(JsonConvert.SerializeObject(ordine), JsonConvert.SerializeObject(retrievedValue)))
                        {
                          // Replace the old value with the new value.
                          if (!_UserInterfaceService.QryOrdini.TryUpdate(ordine.IdOrdine, ordine, retrievedValue))
                          {
                            // The data was not updated. Log error, throw exception, etc.
                            Log.Error($"DatabaseTimerService - GetDatabaseData - Sincronizzazione QryOrdini - {ordine.IdOrdine} non aggiornato");
                          }
                        }
                      }
                      else
                      {
                        if (!_UserInterfaceService.QryOrdini.TryAdd(ordine.IdOrdine, ordine))
                        {
                          Log.Error($"DatabaseTimerService - GetDatabaseData - Sincronizzazione QryOrdini - {ordine.IdOrdine} non inserito");
                        }
                      }
                    }

                    foreach (var riga in _qryOrdiniRighe)
                    {
                      if (_UserInterfaceService.QryOrdiniRighe.TryGetValue(new Tuple<long, int>(riga.IdOrdine, riga.IdRiga), out ArchOrdiniRighe retrievedValue))
                      {
                        // If something changes - Make a copy of the data. 
                        if (!JToken.DeepEquals(JsonConvert.SerializeObject(riga), JsonConvert.SerializeObject(retrievedValue)))
                        {
                          // Replace the old value with the new value.
                          if (!_UserInterfaceService.QryOrdiniRighe.TryUpdate(new Tuple<long, int>(riga.IdOrdine, riga.IdRiga), riga, retrievedValue))
                          {
                            // The data was not updated. Log error, throw exception, etc.
                            Log.Error($"DatabaseTimerService - GetDatabaseData - Sincronizzazione QryOrdiniRighe - {riga.IdOrdine}/{riga.IdRiga} non aggiornata");
                          }
                        }
                      }
                      else
                      {
                        if (!_UserInterfaceService.QryOrdiniRighe.TryAdd(new Tuple<long, int>(riga.IdOrdine, riga.IdRiga), riga))
                        {
                          Log.Error($"DatabaseTimerService - GetDatabaseData - Sincronizzazione QryOrdiniRighe - {riga.IdOrdine}/{riga.IdRiga}  non inserita");
                        }
                      }
                    }
          */

          #region Ordini Appena Nati
          /*
                    // Elaborazione degli ordini appena nati
                    List<ArchOrdini> qryOrdiniAppenaNati = _UserInterfaceService.QryOrdini.Where(w => w.Value.IdStatoOrdine == 0).OrderBy(o => o.Key).Select(s => s.Value).ToList();

                    // Se esiste almeno un ordine "appena nato"
                    foreach (var Ordine in qryOrdiniAppenaNati)
                    {
                      // Se l'ordine non ha righe lo escludo da ulteriori logiche (oppure lo elimino)
                      if (!_UserInterfaceService.QryOrdiniRighe.Where(w => w.Key.Item1 == Ordine.IdOrdine).Any())
                      {
                        await _FesteDataAccess.DeleteArchOrdiniAsync(Ordine); // Elimino l'Ordine dal Database - // Ordine.IdStatoOrdine = 4;
                        _UserInterfaceService.QryOrdini.TryRemove(Ordine.IdOrdine, out ArchOrdini _Ordine);  // Elimino l'Ordine dalla struttura di memoria
                      }
                      else
                      {
                        var Cassa = _UserInterfaceService.AnagrCasse.Where(w => w.IdCassa == Ordine.IdCassa).FirstOrDefault();

                        if (Cassa.SoloBanco.Value)
                        {
                          // Se l'ordine arriva da una cassa "SoloBanco" devo evadere l'ordine
                          await EvadiOrdineAsync(Ordine);
                        }
                        else
                        {
                          // Devo evadere tutte le liste che non sono gestite - TODO
                          foreach (var item in _UserInterfaceService.AnagrListe.Where(w => w.Visibile == false))
                          {
                            if (_UserInterfaceService.QryOrdiniRighe.Values.Where(w => w.IdOrdine == Ordine.IdOrdine).Select(s => s.IdCategoria).Contains(item.IdLista))
                              await EvadiListaNonGestitaAsync(Ordine, item);
                          }
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
          */
          #endregion

          #region Sincronizzazione AnagrProdotti
          try
          {
            bool MustNotify = false;
            var _qryStatoProdotti = (from p in _qryOrdiniRighe // .Where(w => w.IdStatoRiga == 3)
                                     group p by p.IdProdotto into grp
                                     orderby grp.Key
                                     select new
                                     {
                                       IdProdotto = grp.Key,
                                       QuantitàProdotto = (uint)grp.Sum(t => t.QuantitàProdotto),
                                       QuantitàEvasa = (uint)grp.Sum(t => t.QuantitàEvasa),
                                     }).ToList();

            var _qryStatoProdottiCum = (from p in _UserInterfaceService.AnagrProdotti.Values
                                        join r in _qryOrdiniRighe.Where(w => w.IdStatoRiga == (int)K_STATO_RIGA.OrdineEvaso) on p.IdProdotto equals r.IdProdotto
                                        group new { p, r } by p.EvadiSuIdProdotto into grp
                                        select new
                                        {
                                          IdProdotto = grp.Key,
                                          EvasoCumulativo = (uint)grp.Sum(t => t.r.QuantitàEvasa),
                                          ConsumoCumulativo = (uint)grp.Sum(t => t.r.QuantitàProdotto),
                                        }).ToList();

            foreach (var item in _qryStatoProdotti)
            {
              bool IsToBeUpdated1 = false;
              bool IsToBeUpdated2 = false;

              if (_UserInterfaceService.AnagrProdotti.TryGetValue(item.IdProdotto, out AnagrProdotti p))
              {
                IsToBeUpdated1 = (p.Consumo != item.QuantitàProdotto || p.Evaso != item.QuantitàEvasa);
                if (IsToBeUpdated1)
                {
                  p.Consumo = item.QuantitàProdotto;
                  p.Evaso = item.QuantitàEvasa;

                  MustNotify = true;
                }

                var a = _qryStatoProdottiCum.Where(w => w.IdProdotto == item.IdProdotto).FirstOrDefault();
                if (a != null)
                {
                  IsToBeUpdated2 = (p.ConsumoCumulativo != a.ConsumoCumulativo || p.EvasoCumulativo != a.EvasoCumulativo);
                  if (IsToBeUpdated2)
                  {
                    p.ConsumoCumulativo = a.ConsumoCumulativo;
                    p.EvasoCumulativo = a.EvasoCumulativo;

                    MustNotify = true;
                  }
                }
                if (IsToBeUpdated1 || IsToBeUpdated2)
                {
                  await _FesteDataAccess.UpdateAnagrProdottiAsync(p);
                }
              }
            }
            if (MustNotify)
              _UserInterfaceService.OnNotifyStatoProdotti(new DatiNotifyStatoProdotti
              {
                idCassa = 0,
                statoProdotti = _UserInterfaceService.AnagrProdotti.Values.ToList()
              });
          }
          catch (Exception ex)
          {
            Log.Error(ex, "DatabaseTimerService - GetDatabaseData - Sincronizzazione AnagrProdotti");
          }
          #endregion
        }
        else
        {
          _UserInterfaceService.QryOrdini.Clear();
          _UserInterfaceService.QryOrdiniRighe.Clear();

          // Sincronizzazione AnagrProdotti
          foreach (var item in _UserInterfaceService.AnagrProdotti.Values)
          {
            bool IsToBeUpdated = false;

            IsToBeUpdated = (
              item.Consumo != 0 ||
              item.Evaso != 0 ||
              item.ConsumoCumulativo != 0 ||
              item.EvasoCumulativo != 0
              );

            if (IsToBeUpdated)
            {
              item.Consumo = 0;
              item.Evaso = 0;
              item.ConsumoCumulativo = 0;
              item.EvasoCumulativo = 0;

              await _FesteDataAccess.UpdateAnagrProdottiAsync(item);
            }
          }
        }
      }
      catch (Exception ex)
      {
        Log.Error(ex, "DatabaseTimerService - GetDatabaseData");
        //Log.Information($"_UserInterfaceService {(_UserInterfaceService != null ? "is not" : " is ")} null");
      }
      watch.Stop();
      return (watch.ElapsedMilliseconds);
    }
    /*
        public async Task EvadiOrdineAsync(ArchOrdini _ordine)
        {
          //var OrdinePre = _ordine;

          _ordine.IdStatoOrdine = (int)K_STATO_ORDINE.InCorso; //  2;
          _ordine.DataOra = DateTime.Now;

          // Aggiorno il record dell'Ordine nel Database
          await _FesteDataAccess.UpdateArchOrdiniAsync(_ordine);

          // Tutte le righe dell'ordine vengono messe nello stato 3
          foreach (var item in _UserInterfaceService.QryOrdiniRighe.Values.Where(w => w.IdOrdine == _ordine.IdOrdine))
          {
            item.IdStatoRiga = (int)K_STATO_RIGA.OrdineEvaso; // 3;
            item.QuantitàEvasa = item.QuantitàProdotto;
            item.DataOra_RigaPresaInCarico = DateTime.Now;
            item.DataOra_RigaEvasa = DateTime.Now;

            await _FesteDataAccess.UpdateArchOrdiniRigheAsync(item);
          }
        }

        public async Task EvadiListaNonGestitaAsync(ArchOrdini _ordine, AnagrListe _lista)
        {
          // Tutte le righe dell'ordine che appartengono alla lista vengono messe nello stato 3 (Ordine Evaso x questa lista)
          var Prodotti = (_UserInterfaceService.AnagrProdotti.Values.Where(w => w.IdLista == _lista.IdLista)).ToList();

          var Prodotto = Prodotti.Select(s => s.IdProdotto).ToArray();

          foreach (var item in _UserInterfaceService.QryOrdiniRighe.Values.Where(w => (w.IdOrdine == _ordine.IdOrdine) && Prodotto.Contains(w.IdProdotto)))
          {
            item.IdStatoRiga = (int)K_STATO_RIGA.OrdineEvaso; // 3; 
            item.QuantitàEvasa = item.QuantitàProdotto;
            item.DataOra_RigaPresaInCarico = DateTime.Now;
            item.DataOra_RigaEvasa = DateTime.Now;

            await _FesteDataAccess.UpdateArchOrdiniRigheAsync(item);
          }

          // Se la lista è una lista padre (IoSonoListaPadre = true)
          if (_lista.IoSonoListaPadre == true)
          {
            var OrdinePre = _ordine;

            // Sblocco le liste figlio
            _ordine.IdStatoOrdine = (int)K_STATO_ORDINE.InCorso; //  2;

            await _FesteDataAccess.UpdateArchOrdiniAsync(_ordine);

            // Aggiorno il record dell'Ordine nella struttura di memoria
            var index = _UserInterfaceService.QryOrdini.TryUpdate(_ordine.IdOrdine, _ordine, OrdinePre);

            // Tutte le righe dell'ordine che fanno parte delle liste figlio devo essere riconoscibili - Metto il loro IdStatoRiga a -1
            var ListeFiglio = _UserInterfaceService.AnagrListe.Where(w => w.IdListaPadre == _lista.IdLista).Select(s => s.IdLista).ToArray();
            var RigheDelleListeFiglio = (from r in _UserInterfaceService.QryOrdiniRighe.Values.Where(w => (w.IdOrdine == _ordine.IdOrdine))
                                         join p in Prodotti.Where(w => ListeFiglio.Contains(w.IdLista)) on r.IdProdotto equals p.IdProdotto
                                         select r.IdRiga).ToArray();

            foreach (var item in _UserInterfaceService.QryOrdiniRighe.Values.Where(w => (w.IdOrdine == _ordine.IdOrdine) && RigheDelleListeFiglio.Contains(w.IdRiga)))
            {
              item.IdStatoRiga = -1;

              await _FesteDataAccess.UpdateArchOrdiniRigheAsync(item);
            }
          }
          _UserInterfaceService.OnNotifyStatoOrdine(_ordine.IdOrdine);
          _UserInterfaceService.OnNotifyStatoProdotti(_ordine.IdCassa);
        }

        private int GetStatoOrdine(ArchOrdini archOrdine)
        {
          int Result = (int)K_STATO_ORDINE.InCorso; // 2;

          var ArchOrdiniRighe = _UserInterfaceService.QryOrdiniRighe.Where(w => w.Key.Item1 == archOrdine.IdOrdine).ToList();

          // Verifico se esistono righe dell'ordine con IdListaPadre > 0 (cioè con delle code a priorità maggiore ancora da smaltire)
          var RigheConListaPadre = from r in ArchOrdiniRighe
                                   join p in _UserInterfaceService.AnagrProdotti.Values on r.Value.IdProdotto equals p.IdProdotto
                                   join l in _UserInterfaceService.AnagrListe.Where(w => w.IdListaPadre > 0) on p.IdLista equals l.IdLista
                                   select new { r, p, l };

          foreach (var Riga in RigheConListaPadre)
          {
            // Verifico se esistono ancora prodotti da smaltire nella lista padre
            var RigheListaPadre = from r in ArchOrdiniRighe.Where(w => w.Value.IdStatoRiga < (int)K_STATO_RIGA.OrdineEvaso) //  3) 
                                  join p in _UserInterfaceService.AnagrProdotti.Values on r.Value.IdProdotto equals p.IdProdotto
                                  join l in _UserInterfaceService.AnagrListe.Where(w => w.IdLista == Riga.l.IdListaPadre) on p.IdLista equals l.IdLista
                                  select new { r, p, l };

            // Me ne basta una per decidere che lo stato dell'ordine é = 1 (Bloccato dalla lista Padre)
            if (RigheListaPadre.Any())
            {
              Result = (int)K_STATO_ORDINE.Bloccato; // 1; // Le liste con priorità = 2 devono aspettare
              break;
            }
          }
          return Result;
        }
    */
  }
}
