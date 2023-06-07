using Dapper;
using Dapper.Contrib.Extensions;
using MySql.Data.MySqlClient;
using System.Data;

namespace BlazorFeste.Util.DataAccess
{
  public class MySQLDataAccess<T>
  {
    private string _connectionString;
    public MySQLDataAccess(string connectionString) => _connectionString = connectionString;

    public async Task<IEnumerable<T>> QueryListAsync(string Sql, object Parameters = default, CancellationToken ct = default)
    {
      IEnumerable<T> retList = new List<T>();
      using (var con = new MySqlConnection(_connectionString))
      {
        var command = new CommandDefinition(Sql, Parameters, commandTimeout: 15, cancellationToken: ct);
        if (con.State == ConnectionState.Closed) { await con.OpenAsync(ct); }
        try { retList = await con.QueryAsync<T>(command); }
        catch (TaskCanceledException tEx) { _ = tEx; }
        catch (Exception) { throw; }
        finally { if (con.State == ConnectionState.Open) { con.Close(); } }
      }
      return retList;
    }
    public async Task<T> QuerySingleAsync(string Sql, object Parameters = default, CancellationToken ct = default)
    {
      var retObj = default(T);
      using (var con = new MySqlConnection(_connectionString))
      {
        var command = new CommandDefinition(Sql, Parameters, commandTimeout: 15, cancellationToken: ct);
        if (con.State == ConnectionState.Closed) { await con.OpenAsync(ct); }
        try { retObj = await con.QueryFirstAsync<T>(command); }
        catch (TaskCanceledException tEx) { _ = tEx; }
        catch (Exception) { throw; }
        finally { if (con.State == ConnectionState.Open) { con.Close(); } }
      }
      return retObj;
    }
    public async Task<int> ExecuteStatementAsync(string Sql, object Parameters = default, CancellationToken ct = default)
    {
      int result = 0;
      using (var con = new MySqlConnection(_connectionString))
      {
        var command = new CommandDefinition(Sql, Parameters, cancellationToken: ct);
        if (con.State == ConnectionState.Closed) { await con.OpenAsync(ct); }
        try { result = await con.ExecuteAsync(command); }
        catch (TaskCanceledException tEx) { _ = tEx; }
        catch (Exception) { throw; }
        finally { if (con.State == ConnectionState.Open) { con.Close(); } }
      }
      return result;
    }
    public int ExecuteStatement(string Sql, object Parameters = default, int timeout = 30, CancellationToken ct = default)
    {
      int result = 0;
      using (var con = new MySqlConnection(_connectionString))
      {
        var command = new CommandDefinition(Sql, Parameters, commandTimeout: timeout, cancellationToken: ct);
        if (con.State == ConnectionState.Closed) { con.Open(); }
        try { result = con.Execute(command); }
        catch (TaskCanceledException tEx) { _ = tEx; }
        catch (Exception) { throw; }
        finally { if (con.State == ConnectionState.Open) { con.Close(); } }
      }
      return result;
    }
  }
}
