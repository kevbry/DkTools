using DKX.Compilation.Project.Bson;
using DKX.Compilation.ReportItems;
using DKX.Compilation.Resolving;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace DKX.Compilation.Project
{
    public interface IProject
    {
        void OnCompilePhaseStarted(CompilePhase phase);

        void OnFileScanCompleted(CompilePhase phase, string dkxPathName, IEnumerable<INamespace> namespaces);

        Task OnCompilePhaseCompleted(CompilePhase phase, IReportItemCollector report);

        DateTime GetCompileTimeStamp(CompilePhase phase, string dkxPathName);

        IEnumerable<string> GetFileDependencies(string dkxPathName);

        IEnumerable<TableHash> GetTableDependencies(string dkxPathName);

        void OnCompileCompleted(string dkxPathName, IEnumerable<string> fileDependencies, IEnumerable<TableHash> tableDependencies);

        INamespace GetNamespaceOrNull(string namespaceName);

        IClass GetClassByFullNameOrNull(string fullClassName);
    }

    public struct TableHash
    {
        public string TableName { get; private set; }
        public string Hash { get; private set; }

        public TableHash(string tableName, string hash)
        {
            TableName = tableName ?? throw new ArgumentNullException(nameof(tableName));
            Hash = hash ?? throw new ArgumentNullException(nameof(hash));
        }

        public static TableHash[] EmptyArray = new TableHash[0];

        public void Serialize(BinaryWriter bin)
        {
            bin.Write(TableName ?? string.Empty);
            bin.Write(Hash ?? string.Empty);
        }

        public static TableHash Deserialize(BinaryReader bin)
        {
            var tableName = bin.ReadString();
            var hash = bin.ReadString();
            return new TableHash(tableName, hash);
        }

        public BsonObject ToBson(BsonFile bson)
        {
            return new BsonObject(bson,
                "TableName", new BsonString(bson, TableName),
                "Hash", new BsonString(bson, Hash));
        }

        public static TableHash FromBson(BsonNode node)
        {
            if (!(node is BsonObject obj)) throw new InvalidBsonTypeException();
            return new TableHash(obj.GetString("TableName"), obj.GetString("Hash"));
        }
    }
}
