using DK.Code;
using DK.Definitions;
using DK.Modeling;
using DK.Scanning;
using System;
using System.Collections.Generic;

namespace DK.Repository
{
    public interface IAppRepo
    {
        IEnumerable<string> GetDependentFiles(string fileName, int maxResults = 0);

        IEnumerable<Definition> GetGlobalDefinitions();

        IEnumerable<ClassDefinition> GetClassDefinitions(string className);

        IEnumerable<ExtractTableDefinition> GetPermanentExtractDefinitions(string extractName);

        IEnumerable<FunctionDefinition> SearchForFunctionDefinitions(string funcName);

        IEnumerable<FilePosition> FindAllReferences(string extRefId);

        bool TryGetFileDate(string fileName, out DateTime modified);

        void UpdateFile(CodeModel model, FFScanMode scanMode);

        void ResetScanDateOnDependentFiles(string fileName);

        void ResetScanDateOnFile(string fileName);

        void OnExportsComplete();

        void OnScanComplete();
    }
}
