﻿using Dapper;
using Dapper.Contrib.Extensions;
using MySql.Data.MySqlClient;
using BlazorFeste.Services;
using System.Data;
using BlazorFeste.Data.Models;
using Serilog;
using BlazorFeste.Classes;
using Newtonsoft.Json;

namespace BlazorFeste.DataAccess
{
  public class FesteDataAccess
  {
    private readonly UserInterfaceService _UserInterfaceService;
    private readonly IWebHostEnvironment _Env;

    //private readonly string _MySQL_connectionString = "Server=localhost;Database=cassa_feste;User=cassa_feste;Password=cassa_feste;Convert Zero Datetime=True;SslMode=none";
    private readonly string _MySQL_connectionString = "Server=localhost;Database=BlazorFeste;User=BlazorFeste;Password=BlazorFeste;Convert Zero Datetime=True;SslMode=none";

    public FesteDataAccess(UserInterfaceService userInterfaceService, IWebHostEnvironment env)
    {
      _UserInterfaceService = userInterfaceService;
      _Env = env;
    }
    public async Task<IEnumerable<T>> GetFromMySQLAsync<T>(CancellationToken ct = default) where T : class
    {
      IEnumerable<T> retList = new List<T>();
      using (var con = new MySqlConnection(_MySQL_connectionString))
      {
        if (con.State == ConnectionState.Closed)
          con.Open();
        try
        {
          retList = await con.GetAllAsync<T>();
        }
        catch (Exception ex)
        {
          Log.Error($"GetFromMySQLAsync - {retList.GetType()} - {ex.Message}");
        }
        finally
        {
          if (con.State == ConnectionState.Open)
            con.Close();
        }
      }
      return retList;
    }
    public async Task<IEnumerable<T>> GetGenericQuery<T>(string commandText, object parameters = null, CancellationToken ct = default)
    {
      IEnumerable<T> retList = new List<T>();

      using (var con = new MySqlConnection(_MySQL_connectionString))
      {
        if (con.State == ConnectionState.Closed)
          con.Open();
        try
        {
          if (parameters != null)
          {
            var command = new CommandDefinition(commandText, parameters, cancellationToken: ct);
            retList = await con.QueryAsync<T>(command);
          }
          else
          {
            retList = await con.QueryAsync<T>(commandText);
          }
        }
        catch (Exception ex)
        {
          Log.Error($"GetGenericQuery - {retList.GetType()} - {ex.Message}");
        }
        finally
        {
          if (con.State == ConnectionState.Open)
            con.Close();
        }
      }
      return retList;
    }
    public async Task<IEnumerable<ArchFeste>> GetArchFesteAsync(DateTime dtFestaInCorso, CancellationToken ct = default)
    {
      IEnumerable<ArchFeste> retList = new List<ArchFeste>();
      // ArchFeste DatiFesta = db.ArchFeste.Where(w => (dtFestaInCorso >= w.DataInizio && dtFestaInCorso <= w.DataFine)).FirstOrDefault();
      using (var con = new MySqlConnection(_MySQL_connectionString))
      {
        const string sql = @"SELECT * FROM arch_feste WHERE DataInizio <= @dtFestaInCorso AND DataFine >= @dtFestaInCorso";
        var Params = new { dtFestaInCorso = dtFestaInCorso };
        var command = new CommandDefinition(sql, Params, cancellationToken: ct);

        if (con.State == ConnectionState.Closed)
          con.Open();
        try
        {
          retList = await con.QueryAsync<ArchFeste>(command);
        }
        catch (Exception ex)
        {
          Log.Error($"GetArchFesteAsync - {retList.GetType()} - {ex.Message}");
          //_ = ex;
        }
        finally
        {
          if (con.State == ConnectionState.Open)
            con.Close();
        }
      }
      return retList;
    }
    //public async Task<int> GetStatoOrdineAsync(ArchFeste datiFesta, ArchOrdini archOrdine, CancellationToken ct = default)
    //{
    //  int Result = 2;
    //  using (var con = new MySqlConnection(_MySQL_connectionString))
    //  {
    //    const string sql = @"SELECT g.idlistapadre as IdListaPadre, COUNT(g.idlistapadre) as numeroRecords FROM (" +
    //      "SELECT idlistapadre FROM arch_ordini_righe r " +
    //      "JOIN anagr_prodotti p ON p.IdProdotto = r.IdProdotto " +
    //      "JOIN anagr_liste l ON l.IdLista = p.IdLista " +
    //      "WHERE r.IdOrdine = @IdOrdine AND p.idListino = @IdListino AND l.idListino = @IdListino AND l.IdListaPadre > 0 " +
    //      ") g GROUP BY idlistapadre";

    //    var Params = new { IdOrdine = archOrdine.IdOrdine, IdListino = datiFesta.IdListino };
    //    var command = new CommandDefinition(sql, Params, cancellationToken: ct);

    //    if (con.State == ConnectionState.Closed)
    //      con.Open();
    //    try
    //    {
    //      var retList = await con.QueryAsync(command);
    //      foreach (var item in retList)
    //      {
    //        // Verifico se esistono ancora prodotti da smaltire nella lista padre
    //        const string sql2 = @"SELECT * FROM arch_ordini_righe r " +
    //                  "JOIN anagr_prodotti p ON p.IdProdotto = r.IdProdotto " +
    //                  "JOIN anagr_liste l ON l.IdLista = p.IdLista " +
    //                  "WHERE r.IdOrdine = @IdOrdine AND r.IdStatoRiga < 3 AND p.idListino = @IdListino AND l.idListino = @IdListino AND l.IdLista = @IdListaPadre ";
    //        var Params2 = new { IdOrdine = archOrdine.IdOrdine, IdListino = datiFesta.IdListino, IdListaPadre = item.IdListaPadre };
    //        var command2 = new CommandDefinition(sql2, Params2, cancellationToken: ct);

    //        var retList2 = await con.QueryAsync(command2);

    //        // Me ne basta una per decidere che lo stato dell'ordine é = 1
    //        if (retList2.Any())
    //        {
    //          Result = 1;
    //          break;
    //        }
    //      }
    //    }
    //    catch (Exception ex)
    //    {
    //      _ = ex;
    //    }
    //    finally
    //    {
    //      if (con.State == ConnectionState.Open)
    //        con.Close();
    //    }
    //  }
    //  return Result;
    //}
    public async Task<int> DeleteArchOrdiniAsync(ArchOrdini archOrdine, CancellationToken ct = default)
    {
      using (var con = new MySqlConnection(_MySQL_connectionString))
      {
        if (con.State == ConnectionState.Closed)
          con.Open();
        try
        {
          var retList = await con.DeleteAsync<ArchOrdini>(archOrdine);
        }
        catch (Exception ex)
        {
          Log.Error($"DeleteArchOrdiniAsync - {ex.Message}");
          //_ = ex;
        }
        finally
        {
          if (con.State == ConnectionState.Open)
            con.Close();
        }
      }
      return 1;
    }
    public async Task<int> UpdateArchOrdiniAsync(ArchOrdini archOrdine, CancellationToken ct = default)
    {
      using (var con = new MySqlConnection(_MySQL_connectionString))
      {
        if (con.State == ConnectionState.Closed)
          con.Open();
        try
        {
          var retList = await con.UpdateAsync<ArchOrdini>(archOrdine);
        }
        catch (Exception ex)
        {
          Log.Error($"UpdateArchOrdiniAsync - {ex.Message}");
          //_ = ex;
        }
        finally
        {
          if (con.State == ConnectionState.Open)
            con.Close();
        }
      }
      return 1;
    }
    public async Task<int> UpdateArchOrdiniRigheAsync(ArchOrdiniRighe archOrdineRiga, CancellationToken ct = default)
    {
      using (var con = new MySqlConnection(_MySQL_connectionString))
      {
        if (con.State == ConnectionState.Closed)
          con.Open();
        try
        {
          var retList = await con.UpdateAsync<ArchOrdiniRighe>(archOrdineRiga);
        }
        catch (Exception ex)
        {
          Log.Error($"UpdateArchOrdiniRigheAsync - {ex.Message}");
          //_ = ex;
        }
        finally
        {
          if (con.State == ConnectionState.Open)
            con.Close();
        }
      }
      return 1;
    }
    public async Task<int> InsertArchOrdiniAsync(ArchOrdini archOrdine, List<ArchOrdiniRighe> archOrdiniRighe, CancellationToken ct = default)
    {
      int idOrdine = -1;
      using (var con = new MySqlConnection(_MySQL_connectionString))
      {
        if (con.State == ConnectionState.Closed)
          con.Open();
        try
        {
          using (var transaction = await con.BeginTransactionAsync())
          {
            string sql = @"INSERT INTO arch_ordini(Cassa, DataOra, TipoOrdine, Tavolo, NumeroCoperti, Referente, NoteOrdine) 
            VALUES(@Cassa, @DataOra, @TipoOrdine, @Tavolo, @NumeroCoperti, @Referente, @NoteOrdine); SELECT LAST_INSERT_ID();";

            var Params1 = new
            {
              Cassa = archOrdine.Cassa,
              DataOra = archOrdine.DataOra,
              TipoOrdine = archOrdine.TipoOrdine,
              Tavolo = archOrdine.Tavolo,
              NumeroCoperti = archOrdine.NumeroCoperti,
              Referente = archOrdine.Referente,
              NoteOrdine = archOrdine.NoteOrdine
            };
            idOrdine = await con.ExecuteScalarAsync<int>(sql, Params1, transaction);

            sql = @"INSERT INTO arch_ordini_righe(IdOrdine, IdRiga, IdCategoria, Categoria, IdProdotto, NomeProdotto, IdStatoRiga, QuantitàProdotto, QuantitàEvasa, Importo, DataOra_RigaPresaInCarico, DataOra_RigaEvasa, QueueTicket) 
              VALUES((SELECT LAST_INSERT_ID()), @IdRiga, @IdCategoria, @Categoria, @IdProdotto, @NomeProdotto, @IdStatoRiga, @QuantitàProdotto, @QuantitàEvasa, @Importo, @DataOra_RigaPresaInCarico, @DataOra_RigaEvasa, @QueueTicket)";
            foreach (var item in archOrdiniRighe)
            {
              var Params2 = new
              {
                IdRiga = item.IdRiga,
                IdCategoria = item.IdCategoria,
                Categoria = item.Categoria,
                IdProdotto = item.IdProdotto,
                NomeProdotto = item.NomeProdotto,
                IdStatoRiga = item.IdStatoRiga,
                QuantitàProdotto = item.QuantitàProdotto,
                QuantitàEvasa = item.QuantitàEvasa,
                Importo = item.Importo,
                DataOra_RigaPresaInCarico = item.DataOra_RigaPresaInCarico,
                DataOra_RigaEvasa = item.DataOra_RigaEvasa,
                QueueTicket = item.QueueTicket
              };
              await con.ExecuteAsync(sql, Params2, transaction);
            }
            //Commit transaction
            await transaction.CommitAsync();
          }

#if THREADSAFE
          _UserInterfaceService.QryOrdini.TryAdd(archOrdine.IdOrdine, archOrdine);
          foreach (var element in archOrdiniRighe)
          {
            _UserInterfaceService.QryOrdiniRighe.TryAdd(new Tuple<long, int>( element.IdOrdine, element.IdRiga ), element);
          }
#else
          _UserInterfaceService.QryOrdini.Add(archOrdine);
          _UserInterfaceService.QryOrdiniRighe.AddRange(archOrdiniRighe);
#endif
        }
        catch (Exception ex)
        {
          Log.Error($"InsertArchOrdiniAsync - {ex.Message}");
          //_ = ex;
        }
        finally
        {
          if (con.State == ConnectionState.Open)
            con.Close();
        }
      }
      return (idOrdine);
    }
    public async Task<int> UpdateAnagrProdottiAsync(AnagrProdotti anagrProdotti, CancellationToken ct = default)
    {
      using (var con = new MySqlConnection(_MySQL_connectionString))
      {
        if (con.State == ConnectionState.Closed)
          con.Open();
        try
        {
          var retList = await con.UpdateAsync<AnagrProdotti>(anagrProdotti);
        }
        catch (Exception ex)
        {
          Log.Error($"UpdateAnagrProdottiAsync - {ex.Message}");
          //_ = ex;
        }
        finally
        {
          if (con.State == ConnectionState.Open)
            con.Close();
        }
      }
      return 1;
    }
    public async Task<int> InsertAnagrCasseAsync(AnagrDataChange change, CancellationToken ct = default)
    {
      using (var con = new MySqlConnection(_MySQL_connectionString))
      {
        if (con.State == ConnectionState.Closed)
          con.Open();
        try
        {
          AnagrCasse Cassa = new AnagrCasse();

          JsonConvert.PopulateObject(change.Data.ToString(), Cassa);

          var ElencoCasse = (await con.QueryAsync<int>("SELECT IdCassa FROM anagr_casse WHERE IdListino = @IdListino ORDER BY IdLista ",
            new { IdListino = _UserInterfaceService.ArchFesta.IdListino })).ToList();
          int firstAvailable = Enumerable.Range(1, int.MaxValue)
                                          .Except(ElencoCasse)
                                          .FirstOrDefault();
          Cassa.IdCassa = firstAvailable;
          Cassa.IdListino = _UserInterfaceService.ArchFesta.IdListino;

          var retList = await con.InsertAsync<AnagrCasse>(Cassa);
        }
        catch (Exception ex)
        {
          Log.Error($"InsertAnagrCasseAsync - {ex.Message}");
          //_ = ex;
        }
        finally
        {
          if (con.State == ConnectionState.Open)
            con.Close();
        }
      }
      return 1;
    }
    public async Task<int> UpdateAnagrCasseAsync(AnagrDataChange change, CancellationToken ct = default)
    {
      using (var con = new MySqlConnection(_MySQL_connectionString))
      {
        if (con.State == ConnectionState.Closed)
          con.Open();
        try
        {
          AnagrCasse Cassa = await con.GetAsync<AnagrCasse>(change.Key);
          JsonConvert.PopulateObject(change.Data.ToString(), Cassa);

          var retList = await con.UpdateAsync<AnagrCasse>(Cassa);
        }
        catch (Exception ex)
        {
          Log.Error($"UpdateAnagrListeAsync - {ex.Message}");
          //_ = ex;
        }
        finally
        {
          if (con.State == ConnectionState.Open)
            con.Close();
        }
      }
      return 1;
    }
    public async Task<int> InsertAnagrListeAsync(AnagrDataChange change, CancellationToken ct = default)
    {
      using (var con = new MySqlConnection(_MySQL_connectionString))
      {
        if (con.State == ConnectionState.Closed)
          con.Open();
        try
        {
          AnagrListe Lista = new AnagrListe();

          JsonConvert.PopulateObject(change.Data.ToString(), Lista);

          var ElencoListe = (await con.QueryAsync<int>("SELECT IdLista FROM anagr_liste WHERE IdListino = @IdListino ORDER BY IdLista ",
            new { IdListino = _UserInterfaceService.ArchFesta.IdListino })).ToList();
          int firstAvailable = Enumerable.Range(1, int.MaxValue)
                                          .Except(ElencoListe)
                                          .FirstOrDefault();
          Lista.IdLista = firstAvailable;
          Lista.IdListino = _UserInterfaceService.ArchFesta.IdListino;

          var retList = await con.InsertAsync<AnagrListe>(Lista);
        }
        catch (Exception ex)
        {
          Log.Error($"InsertAnagrListeAsync - {ex.Message}");
          //_ = ex;
        }
        finally
        {
          if (con.State == ConnectionState.Open)
            con.Close();
        }
      }
      return 1;
    }
    public async Task<int> UpdateAnagrListeAsync(AnagrDataChange change, CancellationToken ct = default)
    {
      using (var con = new MySqlConnection(_MySQL_connectionString))
      {
        if (con.State == ConnectionState.Closed)
          con.Open();
        try
        {
          AnagrListe Lista = await con.GetAsync<AnagrListe>(change.Key);
          JsonConvert.PopulateObject(change.Data.ToString(), Lista);

          var retList = await con.UpdateAsync<AnagrListe>(Lista);
        }
        catch (Exception ex)
        {
          Log.Error($"UpdateAnagrListeAsync - {ex.Message}");
          //_ = ex;
        }
        finally
        {
          if (con.State == ConnectionState.Open)
            con.Close();
        }
      }
      return 1;
    }
    public async Task<IEnumerable<ArchOrdini>> GetArchOrdini(CancellationToken ct = default)
    {
      IEnumerable<ArchOrdini> retList = new List<ArchOrdini>();
      var lookup = new Dictionary<long, ArchOrdini>();

      string commandText = "SELECT * FROM arch_ordini o INNER JOIN arch_ordini_righe r ON o.IdOrdine = r.IdOrdine WHERE o.DataAssegnazione = @DataAssegnazione";

      using (var con = new MySqlConnection(_MySQL_connectionString))
      {
        if (con.State == ConnectionState.Closed)
          con.Open();
        try
        {
          retList = (await con.QueryAsync<ArchOrdini, ArchOrdiniRighe, ArchOrdini>(commandText,
            map: (o, r) =>
            {
              r.IdOrdine = o.IdOrdine;
              
              if (lookup.TryGetValue(o.IdOrdine, out ArchOrdini existingOrder))
              {
                o = existingOrder;
              }
              else
              {
                o.righe = new List<ArchOrdiniRighe>();
                lookup.Add(o.IdOrdine, o);
              }
              o.righe.Add(r);
              return o;
            }, splitOn: "IdOrdine",
            param: new { DataAssegnazione = _UserInterfaceService.DtFestaInCorso }
            )).ToList() ;
        }
        catch (Exception ex)
        {
          Log.Error($"GetArchOrdini - {ex.Message}");
          //_ = ex;
        }
        finally
        {
          if (con.State == ConnectionState.Open)
            con.Close();
        }
      }
      return lookup.Select(s => s.Value);
      //return retList;
    }

  }
}
