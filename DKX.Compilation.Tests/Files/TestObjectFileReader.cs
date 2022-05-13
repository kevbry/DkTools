using DKX.Compilation.Files;
using System.Collections.Generic;
using System.Linq;

namespace DKX.Compilation.Tests.Files
{
    class TestObjectFileReader : IObjectFileReader
    {
        private ObjectFileModel _model;

        public TestObjectFileReader(ObjectFileModel model)
        {
            _model = model;
        }

        public TestObjectFileReader(IEnumerable<ObjectFileDependency> fileDepends, IEnumerable<ObjectTableDependency> tableDepends)
        {
            _model = new ObjectFileModel
            {
                FileDependencies = (fileDepends?.Any() ?? false) ? fileDepends.ToArray() : null,
                TableDependencies = (tableDepends?.Any() ?? false) ? tableDepends.ToArray() : null
            };
        }

        public ObjectFileModel GetModel() => _model;

        public IEnumerable<ObjectFileDependency> GetFileDependencies() => _model.FileDependencies ?? ObjectFileDependency.EmptyArray;

        public IEnumerable<ObjectTableDependency> GetTableDependencies() => _model.TableDependencies ?? ObjectTableDependency.EmptyArray;

        public string GetWbdkPathName() => _model.DestinationPathName;

        public string GetDkxPathName() => _model.SourcePathName;
    }
}
