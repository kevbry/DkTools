using DK.AppEnvironment;
using DK.Code;
using DK.Definitions;
using DK.Modeling;
using DK.Scanning;
using System;
using System.Collections.Generic;

namespace DK.Repository
{
    public class NoAppRepo : IAppRepo
    {
        public IEnumerable<string> GetDependentFiles(string fileName, int maxResults = 0) => StringHelper.EmptyStringArray;

        public IEnumerable<Definition> GetGlobalDefinitions() => Definition.EmptyArray;

        public IEnumerable<ClassDefinition> GetClassDefinitions(string className) => ClassDefinition.EmptyArray;

        public IEnumerable<ExtractTableDefinition> GetPermanentExtractDefinitions(string extractName) => ExtractTableDefinition.EmptyArray;

        public IEnumerable<FunctionDefinition> SearchForFunctionDefinitions(string funcName) => FunctionDefinition.EmptyArray;

        public IEnumerable<FilePosition> FindAllReferences(string extRefId) => FilePosition.EmptyArray;

        public bool TryGetFileDate(string fileName, out DateTime modified)
        {
            modified = default;
            return false;
        }

        public void UpdateFile(CodeModel model, FFScanMode scanMode) { }

        public void ResetScanDateOnDependentFiles(string fileName) { }

        public void ResetScanDateOnFile(string fileName) { }

        public void OnExportsComplete() { }

        public void OnScanComplete() { }
    }

    public class NoAppRepoFactory : IAppRepoFactory
    {
        public IAppRepo CreateAppRepo(DkAppSettings appSettings)
        {
            return new NoAppRepo();
        }
    }
}
