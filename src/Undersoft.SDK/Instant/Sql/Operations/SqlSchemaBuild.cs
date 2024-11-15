namespace Undersoft.SDK.Instant.Sql
{
    using Npgsql;
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;

    public enum BuildDbSchemaType
    {
        Schema,
        Temp
    }

    public class SqlSchemaBuild
    {
        private NpgsqlConnection sqlcn;

        public SqlSchemaBuild(NpgsqlConnection _sqlcn)
        {
            sqlcn = _sqlcn;
        }

        public void SchemaPrepare(BuildDbSchemaType buildtype = BuildDbSchemaType.Schema)
        {
            DbSchema dbs = new DbSchema(sqlcn);
            bool wasOpen = false;
            if (sqlcn.State == ConnectionState.Open)
                wasOpen = true;
            if (!wasOpen)
                sqlcn.Open();
            IEnumerable<DataRow> table = sqlcn
                .GetSchema("Tables")
                .Rows.Cast<DataRow>()
                .AsEnumerable()
                .AsQueryable();
            IEnumerable<DataRow> columns = sqlcn
                .GetSchema("Columns")
                .Rows.Cast<DataRow>()
                .AsEnumerable()
                .AsQueryable();
            IEnumerable<DataRow> index = sqlcn
                .GetSchema("IndexColumns")
                .Rows.Cast<DataRow>()
                .AsEnumerable()
                .AsQueryable();
            List<DbTable> dbTables = table
                .Select(
                    r =>
                        new DbTable()
                        {
                            TableName = r["TABLE_NAME"].ToString(),
                            DataDbColumns = new DbColumns()
                            {
                                List = columns
                                    .Where(c => r["TABLE_NAME"].Equals(c["TABLE_NAME"]))
                                    .Select(
                                        k =>
                                            new DbColumn
                                            {
                                                ColumnName = k["COLUMN_NAME"].ToString(),
                                                RubricType = DbNetType.SqlTypeToNet(
                                                    k["DATA_TYPE"].ToString()
                                                ),
                                                MaxLength =
                                                    (k["CHARACTER_MAXIMUM_LENGTH"] != DBNull.Value)
                                                        ? Convert.ToInt32(
                                                            k["CHARACTER_MAXIMUM_LENGTH"]
                                                        )
                                                        : -1,
                                                isDBNull =
                                                    (k["IS_NULLABLE"].ToString() == "YES")
                                                        ? true
                                                        : false,
                                                DbOrdinal = Convert.ToInt32(k["ORDINAL_POSITION"])
                                            }
                                    )
                                    .ToList()
                            },
                            DbPrimaryKey = index
                                .Where(i => r["TABLE_NAME"].Equals(i["table_name"]))
                                .Where(
                                    it =>
                                        columns
                                            .Where(
                                                c =>
                                                    c["TABLE_NAME"].Equals(it["table_name"])
                                                    && c["COLUMN_NAME"].Equals(it["column_name"])
                                            )
                                            .Any()
                                )
                                .Select(
                                    k =>
                                        new DbColumn()
                                        {
                                            ColumnName = k["column_name"].ToString(),
                                            isIdentity =
                                                (k["KeyType"].ToString() == "56") ? true : false,
                                            isKey = true,
                                            DbOrdinal = Convert.ToInt32(k["ordinal_position"]),
                                            RubricType = DbNetType.SqlTypeToNet(
                                                columns
                                                    .Where(
                                                        c =>
                                                            c["TABLE_NAME"].Equals(k["table_name"])
                                                            && c["COLUMN_NAME"].Equals(
                                                                k["column_name"]
                                                            )
                                                    )
                                                    .First()["DATA_TYPE"].ToString()
                                            )
                                        }
                                )
                                .ToArray()
                        }
                )
                .ToList();

            dbs.DataDbTables.AddRange(dbTables.ToList());
            if (buildtype == BuildDbSchemaType.Schema)
                DbHelper.Schema = dbs;
            else
                DbHelper.Temp = dbs;
        }
    }
}
