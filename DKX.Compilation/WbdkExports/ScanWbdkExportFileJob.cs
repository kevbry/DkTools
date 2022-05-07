using DK;
using DK.AppEnvironment;
using DK.Code;
using DK.Definitions;
using DK.Diagnostics;
using DK.Modeling;
using DK.Preprocessing;
using DKX.Compilation.Jobs;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DKX.Compilation.WbdkExports
{
    public class ScanWbdkExportFileJob : ICompileJob
    {
        private DkAppContext _app;
        private string _pathName;
        private string _exportsPathName;
        private FileContext _fileContext;

        public ScanWbdkExportFileJob(DkAppContext app, string pathName, string exportsPathName, FileContext fileContext)
        {
            _app = app ?? throw new ArgumentNullException(nameof(app));
            _pathName = pathName ?? throw new ArgumentNullException(nameof(pathName));
            _exportsPathName = exportsPathName ?? throw new ArgumentNullException(nameof(exportsPathName));
            _fileContext = fileContext;
        }

        public string Description => $"Scan WBDK Exports: {_pathName}";
        public string PathName => _pathName;
        public string ExportsPathName => _exportsPathName;
        public FileContext FileContext => _fileContext;

        public Task ExecuteAsync(CancellationToken cancel)
        {
            _app.Log.Info("Scan: {0} -> {1}", _pathName, _exportsPathName);

            var content = _app.FileSystem.GetFileText(_pathName);

            var source = new CodeSource();
            source.Append(
                text: content,
                fileName: _pathName,
                fileStartPos: 0,
                fileEndPos: content.Length,
                actualContent: true,
                primaryFile: true,
                disabled: false);
            source.Flush();

            var fileStore = new FileStore(_app);
            var model = fileStore.CreatePreprocessedModel(
                appSettings: _app.Settings,
                source: source,
                fileName: _pathName,
                visible: false,
                reason: "WBDK Exports Scan",
                cancel: cancel,
                includeDependencies: null);

            if (_fileContext == FileContext.Function)
            {
                var funcName = PathUtil.GetFileNameWithoutExtension(_pathName);
                var funcDef = model.PreprocessorModel.LocalFunctions
                    .Where(x => x.Definition.Name.EqualsI(funcName))
                    .FirstOrDefault()
                    ?.Definition;
                if (funcDef != null)
                {
                    CreateExportsFile(new FunctionDefinition[] { funcDef }, model.PreprocessorModel.IncludeDependencies);
                }
                else
                {
                    CreateExportsFile(new FunctionDefinition[0], model.PreprocessorModel.IncludeDependencies);
                }
            }
            else
            {
                var funcDefs = model.PreprocessorModel.LocalFunctions
                    .Where(x => x.Definition.Privacy == FunctionPrivacy.Public)
                    .Select(x => x.Definition);
                CreateExportsFile(funcDefs, model.PreprocessorModel.IncludeDependencies);
            }

            return Task.CompletedTask;
        }

        private void CreateExportsFile(IEnumerable<FunctionDefinition> funcDefs, IEnumerable<IncludeDependency> includeDependencies)
        {
            var file = new WbdkExportsModel
            {
                SourceFile = _pathName,
                TimeStamp = DateTime.Now,
                Exports = funcDefs.Any() ? funcDefs.Select(x => new WbdkExport
                {
                    ClassName = x.ClassName,
                    Name = x.Name,
                    DkSignature = x.Signature.ToDbString()
                }).ToArray() : null,
                DependentFiles = includeDependencies.Select(x => x.FileName).ToArray()
            };

            var json = JsonConvert.SerializeObject(file, Formatting.Indented);
            _app.FileSystem.WriteFileText(_exportsPathName, json);
        }
    }
}
