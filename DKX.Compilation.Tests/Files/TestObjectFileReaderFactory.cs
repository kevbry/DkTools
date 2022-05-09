using DKX.Compilation.Files;
using System.Collections.Generic;

namespace DKX.Compilation.Tests.Files
{
    class TestObjectFileReaderFactory : IObjectFileReaderFactory
    {
        private Dictionary<string, ObjectFileDependency[]> _fileDepends = new Dictionary<string, ObjectFileDependency[]>();
        private Dictionary<string, ObjectTableDependency[]> _tableDepends = new Dictionary<string, ObjectTableDependency[]>();

        public IObjectFileReader CreateObjectFileReader(string objectPathName)
        {
            _fileDepends.TryGetValue(objectPathName, out var fileDepends);
            _tableDepends.TryGetValue(objectPathName, out var tableDepends);

            return new TestObjectFileReader(fileDepends, tableDepends);
        }
    }
}
