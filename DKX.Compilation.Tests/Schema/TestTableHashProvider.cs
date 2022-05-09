using DKX.Compilation.Schema;
using System.Collections.Generic;

namespace DKX.Compilation.Tests.Schema
{
    class TestTableHashProvider : ITableHashProvider
    {
        private Dictionary<string, string> _hashes = new Dictionary<string, string>();

        public void SetTableHash(string tableName, string hash)
        {
            _hashes[tableName] = hash;
        }

        public string GetTableHash(string tableName)
        {
            if (_hashes.TryGetValue(tableName, out var hash)) return hash;
            return tableName;
        }
    }
}
