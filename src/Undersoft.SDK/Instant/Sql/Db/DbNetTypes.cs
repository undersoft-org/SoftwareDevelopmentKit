namespace Undersoft.SDK.Instant.Sql
{
    using System;
    using System.Collections.Generic;
    using Undersoft.SDK.Uniques;

    public static class DbNetTypes
    {
        private static Dictionary<Type, object> sqlNetDefaults = new Dictionary<Type, object>()
        {
            { typeof(int), 0 },
            { typeof(string), "" },
            { typeof(DateTime), DateTime.Now },
            { typeof(bool), false },
            { typeof(float), 0 },
            { typeof(decimal), 0 },
            { typeof(Guid), Guid.Empty },
            { typeof(Usid), Usid.Empty },
            { typeof(Uscn), Uscn.Empty }
        };
        private static Dictionary<Type, string> sqlNetTypes = new Dictionary<Type, string>()
        {
            { typeof(byte), "tinyint" },
            { typeof(short), "smallint" },
            { typeof(int), "int" },
            { typeof(string), "nvarchar" },
            { typeof(DateTime), "datetime" },
            { typeof(bool), "bit" },
            { typeof(double), "float" },
            { typeof(float), "numeric" },
            { typeof(decimal), "decimal" },
            { typeof(Guid), "uniqueidentifier" },
            { typeof(long), "bigint" },
            { typeof(byte[]), "varbinary" },
            { typeof(Usid), "bigint" },
            { typeof(Uscn), "varbinary" },
        };

        public static Dictionary<Type, object> SqlNetDefaults
        {
            get { return sqlNetDefaults; }
        }

        public static Dictionary<Type, string> SqlNetTypes
        {
            get { return sqlNetTypes; }
        }
    }
}
