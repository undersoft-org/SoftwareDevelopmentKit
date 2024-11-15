namespace Undersoft.SDK.Instant.Sql
{
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
}
