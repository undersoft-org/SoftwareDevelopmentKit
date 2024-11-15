namespace Undersoft.SDK.Instant.Sql
{
    using Npgsql;
    using System.Collections.Generic;
    using System.Data;
    using Undersoft.SDK.Instant.Series;
    using Undersoft.SDK.Series;

    public class SqlAccessor
    {
        public SqlAccessor() { }

        public IInstantSeries Get(
            string sqlConnectString,
            string sqlQry,
            string tableName,
            ISeries<string> keyNames
        )
        {
            try
            {
                if (DbHelper.Schema == null || DbHelper.Schema.DbTables.Count == 0)
                {
                    _ = new InstantSqlDb(sqlConnectString);
                }
                SqlAdapter sqa = new SqlAdapter(sqlConnectString);

                try
                {
                    return sqa.ExecuteLoad(sqlQry, tableName, keyNames);
                }
                catch (Exception ex)
                {
                    throw new SqlException(ex.ToString());
                }
            }
            catch (Exception ex)
            {
                throw new Exception("see inner exception", ex);
            }
        }

        public object GetSqlDataTable(object parameters)
        {
            try
            {
                Dictionary<string, object> param = new Dictionary<string, object>(
                    (Dictionary<string, object>)parameters
                );
                string sqlQry = param["SqlQuery"].ToString();
                string sqlConnectString = param["ConnectionString"].ToString();

                DataTable Table = new DataTable();
                NpgsqlConnection sqlcn = new NpgsqlConnection(sqlConnectString);
                sqlcn.Open();
                NpgsqlDataAdapter adapter = new NpgsqlDataAdapter(sqlQry, sqlcn);
                adapter.Fill(Table);
                return Table;
            }
            catch (Exception ex)
            {
                throw new Exception("see inner exception", ex);
            }
        }

        public DataTable GetSqlDataTable(NpgsqlCommand cmd)
        {
            try
            {
                DataTable Table = new DataTable();
                if (cmd.Connection.State == ConnectionState.Closed)
                    cmd.Connection.Open();
                NpgsqlDataAdapter adapter = new NpgsqlDataAdapter(cmd);
                adapter.Fill(Table);
                return Table;
            }
            catch (Exception ex)
            {
                throw new Exception("see inner exception", ex);
            }
        }

        public DataTable GetSqlDataTable(string qry, NpgsqlConnection cn)
        {
            try
            {
                DataTable Table = new DataTable();
                if (cn.State == ConnectionState.Closed)
                    cn.Open();
                NpgsqlDataAdapter adapter = new NpgsqlDataAdapter(qry, cn);
                adapter.Fill(Table);
                return Table;
            }
            catch (Exception ex)
            {
                throw new Exception("see inner exception", ex);
            }
        }
    }
}
