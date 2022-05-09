using DK;
using DKX.Compilation.WbdkExports;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DKX.Compilation.Tests.WbdkExports
{
    class TestExportsFileReader : IWbdkExportsFileReader
    {
        private string[] _includeDependencies;
        private WbdkExportTableDependency[] _tableDependencies;

        public TestExportsFileReader(string[] includeDependencies, WbdkExportTableDependency[] tableDependencies)
        {
            _includeDependencies = includeDependencies ?? Constants.EmptyStringArray;
            _tableDependencies = tableDependencies ?? WbdkExportTableDependency.EmptyArray;
        }

        public IEnumerable<string> GetIncludeDependencies() => _includeDependencies;

        public IEnumerable<WbdkExportTableDependency> GetTableDependencies() => _tableDependencies;
    }

    class TestExportsFileReaderFactory : IWbdkExportsFileReaderFactory
    {
        private Dictionary<string, string[]> _exportsByFile = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
        private Dictionary<string, WbdkExportTableDependency[]> _tableDepends = new Dictionary<string, WbdkExportTableDependency[]>(StringComparer.OrdinalIgnoreCase);

        public void SetIncludeDependencies(string exportsPathName, string[] depends)
        {
            _exportsByFile[exportsPathName] = depends;
        }

        public IWbdkExportsFileReader CreateReader(string exportsPathName)
        {
            _exportsByFile.TryGetValue(exportsPathName, out var includeDepends);
            _tableDepends.TryGetValue(exportsPathName, out var tableDepends);

            return new TestExportsFileReader(includeDepends, tableDepends);
        }

        public void SetTableDependency(string exportsPathName, string tableName, string hash)
        {
            if (_tableDepends.TryGetValue(exportsPathName, out var td))
            {
                _tableDepends[exportsPathName] = td
                    .Where(x => x.TableName != tableName)
                    .Concat(new WbdkExportTableDependency[]
                    {
                        new WbdkExportTableDependency
                        {
                            TableName = tableName,
                            Hash = hash
                        }
                    }).ToArray();
                return;
            }
            else
            {
                _tableDepends[exportsPathName] = new WbdkExportTableDependency[]
                {
                    new WbdkExportTableDependency
                    {
                        TableName = tableName,
                        Hash = hash
                    }
                };
            }
        }
    }
}
