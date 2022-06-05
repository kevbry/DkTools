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
        private DateTime _classScanTime = DateTime.MinValue;
        private DateTime _memberScanTime = DateTime.MinValue;
        private DateTime _constantResolutionTime = DateTime.MinValue;
        private DateTime _compileTime = DateTime.MinValue;

        public ProjectFile(string dkxPathName)
        {
            _dkxPathName = dkxPathName ?? throw new ArgumentNullException(nameof(dkxPathName));
        }

        private ProjectFile(string dkxPathName, DateTime classScanTime, DateTime memberScanTime, DateTime constantResolutionTime, DateTime compileTime, string[] fileDeps, TableHash[] tableDeps)
        {
            _dkxPathName = dkxPathName;
            _classScanTime = classScanTime;
            _memberScanTime = memberScanTime;
            _constantResolutionTime = constantResolutionTime;
            _compileTime = compileTime;
            _fileDeps = fileDeps;
            _tableDeps = tableDeps;
        }

        public string DkxPathName => _dkxPathName;
        public IEnumerable<string> FileDependencies { get => _fileDeps ?? DkxConst.EmptyStringArray; set => _fileDeps = value.ToArray(); }
        public IEnumerable<TableHash> TableDependencies { get => _tableDeps; set => _tableDeps = value.ToArray(); }

        public override string ToString() => $"ProjectFile: {_dkxPathName}";

        public DateTime GetCompileTime(CompilePhase phase)
        {
            switch (phase)
            {
                case CompilePhase.ClassScan:
                    return _classScanTime;
                case CompilePhase.MemberScan:
                    return _memberScanTime;
                case CompilePhase.ConstantResolution:
                    return _constantResolutionTime;
                case CompilePhase.FullCompilation:
                    return _compileTime;
                default:
                    throw new InvalidCompilePhaseException();
            }
        }

        public void SetCompileTime(CompilePhase phase, DateTime time)
        {
            switch (phase)
            {
                case CompilePhase.ClassScan:
                    _classScanTime = time;
                    break;
                case CompilePhase.MemberScan:
                    _memberScanTime = time;
                    break;
                case CompilePhase.ConstantResolution:
                    _constantResolutionTime = time;
                    break;
                case CompilePhase.FullCompilation:
                    _compileTime = time;
                    break;
                default:
                    throw new InvalidCompilePhaseException();
            }
        }

        public BsonObject ToBson(BsonFile bson)
        {
            var bsonFile = new BsonObject(bson);

            bsonFile["DkxPathName"] = new BsonString(bson, _dkxPathName);
            bsonFile["ClassScanTime"] = new BsonDateTime(bson, _classScanTime);
            bsonFile["MemberScanTime"] = new BsonDateTime(bson, _memberScanTime);
            bsonFile["ConstantResolutionTime"] = new BsonDateTime(bson, _constantResolutionTime);
            bsonFile["CompileTime"] = new BsonDateTime(bson, _compileTime);
            bsonFile["FileDependencies"] = new BsonArray(bson, (_fileDeps ?? DkxConst.EmptyStringArray).Select(x => new BsonString(bson, x)));
            bsonFile["TableDependencies"] = new BsonArray(bson, (_tableDeps ?? TableHash.EmptyArray).Select(x => x.ToBson(bson)));

            return bsonFile;
        }

        public static ProjectFile FromBson(BsonNode node)
        {
            if (!(node is BsonObject obj)) throw new InvalidBsonTypeException();

            var dkxPathName = obj.GetString("DkxPathName");
            var classScanTime = obj.GetDateTime("ClassScanTime");
            var memberScanTime = obj.GetDateTime("MemberScanTime");
            var constantResolutionTime = obj.GetDateTime("ConstantResolutionTime");
            var compileTime = obj.GetDateTime("CompileTime");
            var fileDeps = obj.GetArray("FileDependencies").Select(x => x.ToString()).ToArray();
            var tableDeps = obj.GetArray("TableDependencies").Select(x => TableHash.FromBson(x)).ToArray();

            return new ProjectFile(dkxPathName, classScanTime, memberScanTime, constantResolutionTime, compileTime, fileDeps, tableDeps);
        }
    }
}
