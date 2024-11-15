namespace Undersoft.SDK.Instant.Sql
{
    using System;
    using System.Globalization;
    using System.Linq;
    using Undersoft.SDK.Logging;
    using Undersoft.SDK.Uniques;

    public static class DbNetType
    {
        public static string NetTypeToSql(Type netType)
        {
            if (DbNetTypes.SqlNetTypes.ContainsKey(netType))
                return DbNetTypes.SqlNetTypes[netType];
            else
                return "varbinary";
        }

        public static object SqlNetVal(
            IInstant fieldRow,
            string fieldName,
            string prefix = "",
            string tableName = null
        )
        {
            object sqlNetVal = new object();
            try
            {
                CultureInfo cci = CultureInfo.CurrentCulture;
                string decRep = (cci.NumberFormat.NumberDecimalSeparator == ".") ? "," : ".";
                string decSep = cci.NumberFormat.NumberDecimalSeparator,
                    _tableName = "";
                if (tableName != null)
                    _tableName = tableName;
                else
                    _tableName = fieldRow.GetType().BaseType.Name;
                if (!DbHelper.Schema.DataDbTables.Have(_tableName))
                    _tableName = prefix + _tableName;
                if (DbHelper.Schema.DataDbTables.Have(_tableName))
                {
                    Type ft = DbHelper.Schema.DataDbTables[_tableName].DataDbColumns[
                        fieldName + "#"
                    ].RubricType;

                    if (DBNull.Value != fieldRow[fieldName])
                    {
                        if (ft == typeof(decimal) || ft == typeof(float) || ft == typeof(double))
                            sqlNetVal = Convert.ChangeType(
                                fieldRow[fieldName].ToString().Replace(decRep, decSep),
                                ft
                            );
                        else if (ft == typeof(string))
                        {
                            int maxLength = DbHelper.Schema.DataDbTables[_tableName].DataDbColumns[
                                fieldName + "#"
                            ].MaxLength;
                            if (fieldRow[fieldName].ToString().Length > maxLength)
                                sqlNetVal = Convert.ChangeType(
                                    fieldRow[fieldName].ToString().Substring(0, maxLength),
                                    ft
                                );
                            else
                                sqlNetVal = Convert.ChangeType(fieldRow[fieldName], ft);
                        }
                        else if (ft == typeof(long) && fieldRow[fieldName] is Usid)
                            sqlNetVal = ((Usid)fieldRow[fieldName]).Id;
                        else if (ft == typeof(byte[]) && fieldRow[fieldName] is Uscn)
                            sqlNetVal = ((Uscn)fieldRow[fieldName]).GetBytes();
                        else
                            sqlNetVal = Convert.ChangeType(fieldRow[fieldName], ft);
                    }
                    else
                    {
                        fieldRow[fieldName] = DbNetTypes.SqlNetDefaults[ft];
                        sqlNetVal = Convert.ChangeType(fieldRow[fieldName], ft);
                    }
                }
                else
                {
                    sqlNetVal = fieldRow[fieldName];
                }
            }
            catch (Exception ex)
            {
                Log.Warning<Instantlog>("Unable to convert sql type to dotnet type", null, ex);
            }
            return sqlNetVal;
        }

        public static Type SqlTypeToNet(string sqlType)
        {
            if (DbNetTypes.SqlNetTypes.ContainsValue(sqlType))
                return DbNetTypes.SqlNetTypes.Where(v => v.Value == sqlType).First().Key;
            else
                return typeof(object);
        }
    }
}
