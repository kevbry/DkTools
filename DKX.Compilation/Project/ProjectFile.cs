using DKX.Compilation.Project.Bson;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DKX.Compilation.Project
{
    class ProjectFile
    {
        private string _dkxPathName;
        private string[] _fileDeps;
        private TableHash[] _tableDeps;
        private DateTime _scanTime = DateTime.MinValue;
        private DateTime _compileTime = DateTime.MinValue;

        public ProjectFile(string dkxPathName)
        {
            _dkxPathName = dkxPathName ?? throw new ArgumentNullException(nameof(dkxPathName));
        }

        private ProjectFile(string dkxPathName, DateTime scanTime, DateTime compileTime, string[] fileDeps, TableHash[] tableDeps)
        {
            _dkxPathName = dkxPathName;
            _scanTime = scanTime;
            _compileTime = compileTime;
            _fileDeps = fileDeps;
            _tableDeps = tableDeps;
        }

        public DateTime CompileTime { get => _compileTime; set => _compileTime = value; }
        public string DkxPathName => _dkxPathName;
        public IEnumerable<string> FileDependencies { get => _fileDeps ?? DkxConst.EmptyStringArray; set => _fileDeps = value.ToArray(); }
        public DateTime PreScanTime { get => _scanTime; set => _scanTime = value; }
        public IEnumerable<TableHash> TableDependencies { get => _tableDeps; set => _tableDeps = value.ToArray(); }

        public BsonObject ToBson(BsonFile bson)
        {
            var bsonFile = new BsonObject(bson);

            bsonFile["DkxPathName"] = new BsonString(bson, _dkxPathName);
            bsonFile["ScanTime"] = new BsonDateTime(bson, _scanTime);
            bsonFile["CompileTime"] = new BsonDateTime(bson, _compileTime);
            bsonFile["FileDependencies"] = new BsonArray(bson, (_fileDeps ?? DkxConst.EmptyStringArray).Select(x => new BsonString(bson, x)));
            bsonFile["TableDependencies"] = new BsonArray(bson, (_tableDeps ?? TableHash.EmptyArray).Select(x => x.ToBson(bson)));

            return bsonFile;
        }

        public static ProjectFile FromBson(BsonNode node)
        {
            if (!(node is BsonObject obj)) throw new InvalidBsonTypeException();

            var dkxPathName = obj.GetString("DkxPathName");
            var scanTime = obj.GetDateTime("ScanTime");
            var compileTime = obj.GetDateTime("CompileTime");
            var fileDeps = obj.GetArray("FileDependencies").Select(x => x.ToString()).ToArray();
            var tableDeps = obj.GetArray("TableDependencies").Select(x => TableHash.FromBson(x)).ToArray();

            return new ProjectFile(dkxPathName, scanTime, compileTime, fileDeps, tableDeps);
        }
    }
}
