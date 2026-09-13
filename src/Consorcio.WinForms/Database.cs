using Microsoft.Data.SqlClient;
using System.Data;

namespace Consorcio;

public sealed class Database(string connectionString)
{
    public SqlConnection Open()
    {
        var c = new SqlConnection(connectionString);
        c.Open();
        return c;
    }
    public DataTable Query(string sql, params SqlParameter[] parameters)
    {
        using var c = Open();
        using var cmd = new SqlCommand(sql, c);
        cmd.Parameters.AddRange(parameters);
        using var reader = cmd.ExecuteReader();
        var data = new DataTable();
        data.Load(reader);
        return data;
    }
    public static SqlParameter P(string name, object? value) => new(name, value ?? DBNull.Value);
}
