using DK;
using DK.AppEnvironment;
using DK.Code;
using DK.Definitions;
using DK.Diagnostics;
using DK.Modeling.Tokens;
using DK.Preprocessing;
using DKX.Compilation.DataTypes;
using DKX.Compilation.Jobs;
using DKX.Compilation.Schema;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DKM = DK.Modeling;

namespace DKX.Compilation.WbdkExports
{
    public class ScanWbdkExportFileJob : ICompileJob
    {
        private DkAppContext _app;
        private string _pathName;
        private string _exportsPathName;
        private FileContext _fileContext;
        private ITableHashProvider _tableHashProvider;

        public ScanWbdkExportFileJob(DkAppContext app, string pathName, string exportsPathName, FileContext fileContext, ITableHashProvider tableHashProvider)
        {
            _app = app ?? throw new ArgumentNullException(nameof(app));
            _pathName = pathName ?? throw new ArgumentNullException(nameof(pathName));
            _exportsPathName = exportsPathName ?? throw new ArgumentNullException(nameof(exportsPathName));
            _fileContext = fileContext;
            _tableHashProvider = tableHashProvider ?? throw new ArgumentNullException(nameof(tableHashProvider));
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

            var fileStore = new DKM.FileStore(_app);
            var model = fileStore.CreatePreprocessedModel(
                appSettings: _app.Settings,
                source: source,
                fileName: _pathName,
                visible: false,
                reason: "WBDK Exports Scan",
                cancel: cancel,
                includeDependencies: null);

            var tableDepends = GetTableDependenciesForModel(model).ToArray();

            if (_fileContext == FileContext.Function)
            {
                var funcName = PathUtil.GetFileNameWithoutExtension(_pathName);
                var funcDef = model.PreprocessorModel.LocalFunctions
                    .Where(x => x.Definition.Name.EqualsI(funcName))
                    .FirstOrDefault()
                    ?.Definition;
                if (funcDef != null)
                {
                    CreateExportsFile(new FunctionDefinition[] { funcDef }, model.PreprocessorModel.IncludeDependencies, tableDepends);
                }
                else
                {
                    CreateExportsFile(new FunctionDefinition[0], model.PreprocessorModel.IncludeDependencies, tableDepends);
                }
            }
            else
            {
                var funcDefs = model.PreprocessorModel.LocalFunctions
                    .Where(x => x.Definition.Privacy == DKM.FunctionPrivacy.Public)
                    .Select(x => x.Definition);
                CreateExportsFile(funcDefs, model.PreprocessorModel.IncludeDependencies, tableDepends);
            }

            return Task.CompletedTask;
        }

        private void CreateExportsFile(IEnumerable<FunctionDefinition> funcDefs, IEnumerable<IncludeDependency> includeDependencies, WbdkExportTableDependency[] tableDependencies)
        {
            var file = new WbdkExportsModel
            {
                SourceFile = _pathName,
                TimeStamp = DateTime.Now,
                Exports = funcDefs.Any() ? funcDefs.Select(f => TransformFunction(f)).ToArray() : null,
                TableDependencies = (tableDependencies?.Any() ?? false) ? tableDependencies : null
            };

            var json = JsonConvert.SerializeObject(file, Formatting.Indented);
            _app.FileSystem.CreateDirectoryRecursive(PathUtil.GetDirectoryName(_exportsPathName));
            _app.FileSystem.WriteFileText(_exportsPathName, json);
        }

        private WbdkExport TransformFunction(FunctionDefinition funcDef)
        {
            var functionExport = new WbdkExport
            {
                ClassName = funcDef.ClassName,
                Name = funcDef.Name,
            };

            if (funcDef.Arguments.Any())
            {
                var args = new List<WbdkExportArgument>();
                var unnamedNumber = 0;
                foreach (var arg in funcDef.Arguments)
                {
                    var dataType = DkDataTypeParser.Parse(new CodeParser(arg.DataType.Source.ToString()));
                    if (dataType == null) dataType = DataType.Unsupported;

                    args.Add(new WbdkExportArgument
                    {
                        Name = !string.IsNullOrEmpty(arg.Name) ? arg.Name : $"unnamed{++unnamedNumber}",
                        DataType = dataType.Value.ToCode(),
                        Ref = arg.PassByMethod == DKM.PassByMethod.Reference || arg.PassByMethod == DKM.PassByMethod.ReferencePlus,
                        Out = false
                    });
                }

                functionExport.Arguments = args.ToArray();
            }

            var returnDataType = DkDataTypeParser.Parse(new CodeParser(funcDef.DataType.Source.ToString()));
            if (returnDataType == null) returnDataType = DataType.Unsupported;
            functionExport.ReturnDataType = returnDataType.Value.ToCode();

            return functionExport;
        }

        private IEnumerable<WbdkExportTableDependency> GetTableDependenciesForModel(DKM.CodeModel model)
        {
            var tablesReferenced = new HashSet<string>();
            foreach (var tableToken in model.File.FindDownward<TableToken>())
            {
                var tableName = tableToken.SourceDefinition.Name;
#if DEBUG
                if (!_app.Settings.Dict.IsTable(tableName)) throw new InvalidOperationException($"Model returned table '{tableName}' definition but it is not a table.");
#endif
                if (!tablesReferenced.Contains(tableName)) tablesReferenced.Add(tableName);
            }

            foreach (var tableName in tablesReferenced.OrderBy(t => t.ToLower()))
            {
                yield return new WbdkExportTableDependency
                {
                    TableName = tableName,
                    Hash = _tableHashProvider.GetTableHash(tableName)
                };
            }
        }
    }
}
