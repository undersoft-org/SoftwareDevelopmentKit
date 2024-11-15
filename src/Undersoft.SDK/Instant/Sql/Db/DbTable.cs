namespace Undersoft.SDK.Instant.Sql
{
    using System.Collections.Generic;
    using System.Linq;
    using Undersoft.SDK.Rubrics;

    public class DbTable
    {
        private DbColumn[] dbPrimaryKey;

        public DbTable() { }

        public DbTable(string tableName)
        {
            TableName = tableName;
        }

        public DbColumns DataDbColumns { get; set; }

        public DbColumn[] DbPrimaryKey
        {
            get { return dbPrimaryKey; }
            set { dbPrimaryKey = value; }
        }

        public List<MemberRubric> GetColumnsForDataTable
        {
            get
            {
                return DataDbColumns.List
                    .Select(
                        c =>
                            new MemberRubric(
                                new FieldRubric(
                                    c.RubricType,
                                    c.ColumnName,
                                    c.DbColumnSize,
                                    c.DbOrdinal
                                )
                                {
                                    RubricSize = c.DbColumnSize
                                }
                            )
                            {
                                FieldId = c.DbOrdinal - 1,
                                IsAutoincrement = c.isAutoincrement,
                                IsDBNull = c.isDBNull,
                                IsIdentity = c.isIdentity
                            }
                    )
                    .ToList();
            }
        }

        public IEnumerable<MemberRubric> GetKeyForDataTable
        {
            get
            {
                return DbPrimaryKey
                    .Select(
                        c =>
                            new MemberRubric(
                                new FieldRubric(
                                    c.RubricType,
                                    c.ColumnName,
                                    c.DbColumnSize,
                                    c.DbOrdinal
                                )
                                {
                                    RubricSize = c.DbColumnSize
                                }
                            )
                            {
                                FieldId = c.DbOrdinal - 1,
                                IsAutoincrement = c.isAutoincrement,
                                IsDBNull = c.isDBNull,
                                IsIdentity = c.isIdentity
                            }
                    );
            }
        }

        public string TableName { get; set; }
    }
}
