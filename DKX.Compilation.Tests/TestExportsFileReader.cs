using DK;
using DKX.Compilation.WbdkExports;
using System;
using System.Collections.Generic;

namespace DKX.Compilation.Tests
{
    class TestExportsFileReader : IWbdkExportsFileReader
    {
        private string[] _includeDependencies;

        public TestExportsFileReader(string[] includeDependencies = null)
        {
            _includeDependencies = includeDependencies ?? Constants.EmptyStringArray;
        }

        public IEnumerable<string> GetIncludeDependencies() => _includeDependencies;
    }

    class TestExportsFileReaderFactory : IWbdkExportsFileReaderFactory
    {
        private Dictionary<string, string[]> _exportsByFile = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);

        public void SetIncludeDependencies(string exportsPathName, string[] depends)
        {
            _exportsByFile[exportsPathName] = depends;
        }

        public IWbdkExportsFileReader CreateReader(string exportsPathName)
        {
            if (_exportsByFile.TryGetValue(exportsPathName, out var depends))
            {
                return new TestExportsFileReader(depends);
            }

            return new TestExportsFileReader();
        }
    }
}
