namespace Undersoft.SDK.Instant.Sql
{
    using Npgsql;
    using System.Collections.Generic;

    public class DbSchema
    {
        public DbSchema(NpgsqlConnection sqlDbConnection)
        {
            DataDbTables = new DbTables();
            DbConfig = new DbConfig(sqlDbConnection.ConnectionString);
        }

        public DbSchema(string dbConnectionString)
        {
            DataDbTables = new DbTables();
            DbConfig = new DbConfig(dbConnectionString);
            SqlDbConnection = new NpgsqlConnection(dbConnectionString);
        }

        public DbTables DataDbTables { get; set; }

        public DbConfig DbConfig { get; set; }

        public string DbName { get; set; }

        public List<DbTable> DbTables
        {
            get { return DataDbTables.List; }
            set { DataDbTables.List = value; }
        }

        public NpgsqlConnection SqlDbConnection { get; set; }
    }
}
