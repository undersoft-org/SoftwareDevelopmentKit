﻿namespace Undersoft.SDK.Instant.Sql
{
    using Npgsql;    
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using Undersoft.SDK.Instant.Series;
    using Undersoft.SDK.Rubrics;
    using Undersoft.SDK.Series;

    public enum BulkPrepareType
    {
        Trunc,
        Drop,
        None
    }

    public enum BulkDbType
    {
        TempDB,
        Origin,
        None
    }

    public class SqlAdapter
    {
        private NpgsqlCommand _cmd;
        private NpgsqlConnection _cn;

        public SqlAdapter(NpgsqlConnection cn)
        {
            _cn = cn;
        }

        public SqlAdapter(string cnstring)
        {
            _cn = new NpgsqlConnection(cnstring);
        }

        public bool DataBulk(
            InstantSeriesItem[] cards,
            string buforTable,
            BulkPrepareType prepareType = BulkPrepareType.None,
            BulkDbType dbType = BulkDbType.TempDB
        )
        {
            try
            {
                IInstantSeries deck = null;
                if (cards.Any())
                {
                    deck = cards.ElementAt(0).InstantSeries;
                    if (_cn.State == ConnectionState.Closed)
                        _cn.Open();
                    try
                    {
                        if (dbType == BulkDbType.TempDB)
                            _cn.ChangeDatabase("tempdb");
                        if (
                            !DbHelper.Temp.DataDbTables.Have(buforTable)
                            || prepareType == BulkPrepareType.Drop
                        )
                        {
                            string createTable = "";
                            if (prepareType == BulkPrepareType.Drop)
                                createTable += "Drop table if exists [" + buforTable + "] \n";
                            createTable += "Generate Table [" + buforTable + "] ( ";
                            foreach (MemberRubric column in deck.Rubrics.AsValues())
                            {
                                string sqlTypeString = "varchar(200)";
                                List<string> defineStr = new List<string>()
                                {
                                    "varchar",
                                    "nvarchar",
                                    "ntext",
                                    "varbinary"
                                };
                                List<string> defineDec = new List<string>()
                                {
                                    "decimal",
                                    "numeric"
                                };
                                int colLenght = column.RubricSize;
                                sqlTypeString = DbNetType.NetTypeToSql(column.RubricType);
                                string addSize =
                                    (colLenght > 0)
                                        ? (defineStr.Contains(sqlTypeString))
                                            ? (string.Format(@"({0})", colLenght))
                                            : (defineDec.Contains(sqlTypeString))
                                                ? (string.Format(@"({0}, {1})", colLenght - 6, 6))
                                                : ""
                                        : "";
                                sqlTypeString += addSize;
                                createTable +=
                                    " [" + column.RubricName + "] " + sqlTypeString + ",";
                            }
                            createTable = createTable.TrimEnd(new char[] { ',' }) + " ) ";
                            NpgsqlCommand createcmd = new NpgsqlCommand(createTable, _cn);
                            createcmd.ExecuteNonQuery();
                        }
                    }
                    catch (SqlException ex)
                    {
                        throw new SqlInsertException(ex.ToString());
                    }
                    if (prepareType == BulkPrepareType.Trunc)
                    {
                        string deleteData = "Truncate Table [" + buforTable + "]";
                        NpgsqlCommand delcmd = new NpgsqlCommand(deleteData, _cn);
                        delcmd.ExecuteNonQuery();
                    }

                    try
                    {
                        object[] values = null;
                        bool first = true;
                        DataReader ndr = new DataReader(cards);
                        using (var writer = _cn.BeginBinaryImport($"COPY [{buforTable}] FROM STDIN BINARY"))
                        {
                            while (ndr.Read())
                            {
                                if (first)
                                    values = new object[ndr.FieldCount];
                                ndr.GetValues(values);
                                writer.WriteRow(values);
                            }
                        }
                    }
                    catch (SqlException ex)
                    {
                        throw new SqlInsertException(ex.ToString());
                    }
                    return true;
                }
                else
                    return false;
            }
            catch (SqlException ex)
            {
                throw new SqlInsertException(ex.ToString());
            }
        }

        public bool DataBulk(
            IInstantSeries deck,
            string buforTable,
            BulkPrepareType prepareType = BulkPrepareType.None,
            BulkDbType dbType = BulkDbType.TempDB
        )
        {
            try
            {
                if (_cn.State == ConnectionState.Closed)
                    _cn.Open();
                try
                {
                    if (dbType == BulkDbType.TempDB)
                        _cn.ChangeDatabase("tempdb");
                    if (
                        !DbHelper.Schema.DataDbTables.Have(buforTable)
                        || prepareType == BulkPrepareType.Drop
                    )
                    {
                        string createTable = "";
                        if (prepareType == BulkPrepareType.Drop)
                            createTable += "Drop table if exists [" + buforTable + "] \n";
                        createTable += "Generate Table [" + buforTable + "] ( ";
                        foreach (MemberRubric column in deck.Rubrics.AsValues())
                        {
                            string sqlTypeString = "varchar(200)";
                            List<string> defineOne = new List<string>()
                            {
                                "varchar",
                                "nvarchar",
                                "ntext",
                                "varbinary"
                            };
                            List<string> defineDec = new List<string>() { "decimal", "numeric" };
                            int colLenght = column.RubricSize;
                            sqlTypeString = DbNetType.NetTypeToSql(column.RubricType);
                            string addSize =
                                (colLenght > 0)
                                    ? (defineOne.Contains(sqlTypeString))
                                        ? (string.Format(@"({0})", colLenght))
                                        : (defineDec.Contains(sqlTypeString))
                                            ? (string.Format(@"({0}, {1})", colLenght - 6, 6))
                                            : ""
                                    : "";
                            sqlTypeString += addSize;
                            createTable += " [" + column.RubricName + "] " + sqlTypeString + ",";
                        }
                        createTable = createTable.TrimEnd(new char[] { ',' }) + " ) ";
                        NpgsqlCommand createcmd = new NpgsqlCommand(createTable, _cn);
                        createcmd.ExecuteNonQuery();
                    }
                }
                catch (SqlException ex)
                {
                    throw new SqlInsertException(ex.ToString());
                }
                if (prepareType == BulkPrepareType.Trunc)
                {
                    string deleteData = "Truncate Table [" + buforTable + "]";
                    NpgsqlCommand delcmd = new NpgsqlCommand(deleteData, _cn);
                    delcmd.ExecuteNonQuery();
                }

                try
                {
                    object[] values = null;
                    bool first = true;
                    DataReader ndr = new DataReader(deck);
                    using (var writer = _cn.BeginBinaryImport($"COPY [{buforTable}] FROM STDIN BINARY"))
                    {
                        while (ndr.Read())
                        {
                            if (first)
                                values = new object[ndr.FieldCount];
                            ndr.GetValues(values);
                            writer.WriteRow(values);
                        }
                    }
                }
                catch (SqlException ex)
                {
                    throw new SqlInsertException(ex.ToString());
                }
                return true;
            }
            catch (SqlException ex)
            {
                throw new SqlInsertException(ex.ToString());
            }
        }

        public int ExecuteDelete(string sqlqry, bool disposeCmd = false)
        {
            if (_cmd == null)
                _cmd = _cn.CreateCommand();
            NpgsqlCommand cmd = _cmd;
            cmd.CommandText = sqlqry;
            NpgsqlTransaction tr = _cn.BeginTransaction();
            cmd.Transaction = tr;
            cmd.Prepare();
            if (_cn.State == ConnectionState.Closed)
                _cn.Open();
            int i = cmd.ExecuteNonQuery();
            tr.Commit();
            if (disposeCmd)
                cmd.Dispose();
            return i;
        }

        public ISeries<ISeries<IInstant>> ExecuteDelete(
            string sqlqry,
            IInstantSeries series,
            bool disposeCmd = false
        )
        {
            if (_cmd == null)
                _cmd = _cn.CreateCommand();
            NpgsqlCommand cmd = _cmd;
            cmd.CommandText = sqlqry;
            cmd.Prepare();
            if (_cn.State == ConnectionState.Closed)
                _cn.Open();
            IDataReader dr = cmd.ExecuteReader();
            SqlReader<IInstantSeries> sr = new SqlReader<IInstantSeries>(dr);
            var _is = sr.ReadDelete(series);
            dr.Dispose();
            if (disposeCmd)
                cmd.Dispose();
            return _is;
        }

        public IInstantSeries ExecuteSelect(string sqlqry, string tableName = null)
        {
            NpgsqlCommand cmd = new NpgsqlCommand(sqlqry, _cn);
            cmd.Prepare();
            if (_cn.State == ConnectionState.Closed)
                _cn.Open();
            IDataReader dr = cmd.ExecuteReader();
            SqlReader<IInstantSeries> sr = new SqlReader<IInstantSeries>(dr);
            IInstantSeries it = sr.ReadSelect(tableName);
            dr.Dispose();
            cmd.Dispose();
            return it;
        }

        public IInstantSeries ExecuteLoad(
            string sqlqry,
            string tableName,
            ISeries<string> keyNames = null
        )
        {
            try
            {
                NpgsqlCommand cmd = new NpgsqlCommand(sqlqry, _cn);
                cmd.Prepare();
                if (_cn.State == ConnectionState.Closed)
                    _cn.Open();
                IDataReader dr = cmd.ExecuteReader();
                SqlReader<IInstantSeries> sdr = new SqlReader<IInstantSeries>(dr);
                IInstantSeries s = sdr.ReadSelect(tableName, keyNames);
                dr.Dispose();
                cmd.Dispose();
                return s;
            }
            catch (Exception ex)
            {
                throw new SqlException(ex.ToString());
            }
        }

        public int ExecuteInsert(string sqlqry, bool disposeCmd = false)
        {
            if (_cmd == null)
                _cmd = _cn.CreateCommand();
            NpgsqlCommand cmd = _cmd;
            cmd.CommandText = sqlqry;
            NpgsqlTransaction tr = _cn.BeginTransaction();
            cmd.Transaction = tr;
            cmd.Prepare();
            if (_cn.State == ConnectionState.Closed)
                _cn.Open();
            int i = cmd.ExecuteNonQuery();
            tr.Commit();
            if (disposeCmd)
                cmd.Dispose();
            return i;
        }

        public ISeries<ISeries<IInstant>> ExecuteInsert(
            string sqlqry,
            IInstantSeries series,
            bool disposeCmd = false
        )
        {
            if (_cmd == null)
                _cmd = _cn.CreateCommand();
            NpgsqlCommand cmd = _cmd;
            cmd.CommandText = sqlqry;
            cmd.Prepare();
            if (_cn.State == ConnectionState.Closed)
                _cn.Open();
            IDataReader dr = cmd.ExecuteReader();
            SqlReader<IInstantSeries> sr = new SqlReader<IInstantSeries>(dr);
            var result = sr.ReadInsert(series);
            dr.Dispose();
            if (disposeCmd)
                cmd.Dispose();
            return result;
        }

        public int ExecuteUpdate(string sqlqry, bool disposeCmd = false)
        {
            if (_cmd == null)
                _cmd = _cn.CreateCommand();
            NpgsqlCommand cmd = _cmd;
            cmd.CommandText = sqlqry;
            NpgsqlTransaction tr = _cn.BeginTransaction();
            cmd.Transaction = tr;
            cmd.Prepare();
            if (_cn.State == ConnectionState.Closed)
                _cn.Open();
            int i = cmd.ExecuteNonQuery();
            tr.Commit();
            if (disposeCmd)
                cmd.Dispose();
            return i;
        }

        public ISeries<ISeries<IInstant>> ExecuteUpdate(
            string sqlqry,
            IInstantSeries cards,
            bool disposeCmd = false
        )
        {
            if (_cmd == null)
                _cmd = _cn.CreateCommand();
            NpgsqlCommand cmd = _cmd;
            cmd.CommandText = sqlqry;
            cmd.Prepare();
            if (_cn.State == ConnectionState.Closed)
                _cn.Open();
            IDataReader sdr = cmd.ExecuteReader();
            SqlReader<IInstantSeries> dr = new SqlReader<IInstantSeries>(sdr);
            var _is = dr.ReadUpdate(cards);
            sdr.Dispose();
            if (disposeCmd)
                cmd.Dispose();
            return _is;
        }
    }
}
