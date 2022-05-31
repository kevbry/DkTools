using DK.AppEnvironment;
using DK.Schema;
using System;
using System.Security.Cryptography;
using System.Text;

namespace DKX.Compilation.Schema
{
    class TableHashProvider : ITableHashProvider
    {
        private DkAppContext _app;
        private SHA256 _sha;

        public TableHashProvider(DkAppContext app)
        {
            _app = app ?? throw new ArgumentNullException(nameof(app));
            _sha = SHA256.Create();
        }

        public string GetTableHash(string tableName)
        {
            var table = _app.Settings.Dict.GetTable(tableName);
            if (table == null) return null;

            var tableSig = GetDkxSignature(table);

            return Convert.ToBase64String(_sha.ComputeHash(Encoding.UTF8.GetBytes(tableSig)));
        }

        private string GetDkxSignature(Table table)
        {
            var sb = new StringBuilder();
            sb.Append(table.Name);
            sb.Append(" {");

            var first = true;
            foreach (var col in table.Columns)
            {
                if (first) first = false;
                else sb.Append(',');
                sb.Append(' ');
                sb.Append(col.Name);
                sb.Append(' ');
                sb.Append(col.DataType.Source);
            }

            sb.Append(" }");
            return sb.ToString();
        }
    }
}
