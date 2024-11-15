namespace Undersoft.SDK.Instant.Sql
{
    using System.Collections.Generic;
    using System.Linq;
    using Undersoft.SDK.Rubrics;

    public class DbColumns
    {
        public DbColumns()
        {
            list = new List<DbColumn>();
        }

        private List<DbColumn> list;
        public List<DbColumn> List
        {
            get { return list; }
            set { list.AddRange(value.Where(c => !this.Have(c.ColumnName)).ToList()); }
        }

        public void Add(DbColumn column)
        {
            if (!this.Have(column.ColumnName))
                List.Add(column);
        }

        public void AddRange(List<DbColumn> _columns)
        {
            list.AddRange(_columns.Where(c => !this.Have(c.ColumnName)).ToList());
        }

        public void Remove(DbColumn column)
        {
            list.Remove(column);
        }

        public void RemoveAt(int index)
        {
            list.RemoveAt(index);
        }

        public void Clear()
        {
            List.Clear();
        }

        public bool Have(string ColumnName)
        {
            return list.Where(c => c.ColumnName == ColumnName).Any();
        }

        public DbColumn this[string ColumnName]
        {
            get { return list.Where(c => ColumnName == c.ColumnName).First(); }
        }
        public DbColumn this[int Ordinal]
        {
            get { return list.Where(c => Ordinal == c.DbOrdinal).First(); }
        }

        public DbColumn GetDbColumn(string ColumnName)
        {
            return list.Where(c => c.ColumnName == ColumnName).First();
        }

        public DbColumn[] GetDbColumns(List<string> ColumnNames)
        {
            return list.Where(c => ColumnNames.Contains(c.ColumnName)).ToArray();
        }

        public List<MemberRubric> GetRubrics(string ColumnNames)
        {
            return list.Where(c => ColumnNames == c.ColumnName).SelectMany(r => r.Rubrics).ToList();
        }

        public List<MemberRubric> GetRubrics(List<string> ColumnNames)
        {
            return list.Where(c => ColumnNames.Contains(c.ColumnName))
                .SelectMany(r => r.Rubrics)
                .ToList();
        }
    }
}
