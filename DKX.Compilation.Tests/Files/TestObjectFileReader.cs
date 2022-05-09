using DKX.Compilation.Files;
using System.Collections.Generic;
using System.Linq;

namespace DKX.Compilation.Tests.Files
{
    class TestObjectFileReader : IObjectFileReader
    {
        private ObjectFileDependency[] _fileDepends;
        private ObjectTableDependency[] _tableDepends;

        public TestObjectFileReader(IEnumerable<ObjectFileDependency> fileDepends, IEnumerable<ObjectTableDependency> tableDepends)
        {
            _fileDepends = (fileDepends?.Any() ?? false) ? fileDepends.ToArray() : ObjectFileDependency.EmptyArray;
            _tableDepends = (tableDepends?.Any() ?? false) ? tableDepends.ToArray() : ObjectTableDependency.EmptyArray;
        }

        public IEnumerable<ObjectFileDependency> GetFileDependencies() => _fileDepends;

        public IEnumerable<ObjectTableDependency> GetTableDependencies() => _tableDepends;
    }
}
