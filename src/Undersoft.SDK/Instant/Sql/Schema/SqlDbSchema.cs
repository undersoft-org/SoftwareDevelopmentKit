namespace Undersoft.SDK.Instant.Sql
{
    using Npgsql;
    using System.Collections.Generic;

    public class DataDbSchema
    {
        public DataDbSchema(NpgsqlConnection sqlDbConnection)
        {
            DataDbTables = new DbTables();
            DbConfig = new DbConfig(sqlDbConnection.ConnectionString);
        }

        public DataDbSchema(string dbConnectionString)
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

    public class DbConfig
    {
        public DbConfig() { }

        public DbConfig(
            string user,
            string password,
            string host,
            string dataBase,
            string provider = "PostgreSql"
        )
        {
            User = user;
            Password = password;
            Provider = provider;
            Host = host;
            Database = dataBase;
            DbConnectionString = string.Format(
                "Host={0};Password={1};Username={2};Database={3}",
                Host,
                Password,
                User,
                Database
            );
        }

        public DbConfig(string dbConnectionString)
        {
            DbConnectionString = dbConnectionString;
        }

        public string Database { get; set; }

        public string DbConnectionString { get; set; }

        public string Password { get; set; }

        public string Provider { get; set; }

        public string Host { get; set; }

        public string User { get; set; }
    }

    public static class DbHand
    {
        public static DataDbSchema Schema { get; set; }

        public static DataDbSchema Temp { get; set; }
    }
}
