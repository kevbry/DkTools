using DK;
using DK.AppEnvironment;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DKX.Compilation.WbdkExports
{
    /// <summary>
    /// Reads a JSON exports file and retrieves stored info.
    /// </summary>
    class WbdkExportsFileReader : IWbdkExportsFileReader
    {
        private DkAppContext _app;
        private string _pathName;
        private WbdkExportsModel _model;

        public WbdkExportsFileReader(DkAppContext app, string pathName)
        {
            _app = app ?? throw new ArgumentNullException(nameof(app));
            _pathName = pathName ?? throw new ArgumentNullException(nameof(pathName));
        }

        private async Task EnsureModelLoadedAsync()
        {
            if (_model == null)
            {
                var json = await _app.FileSystem.ReadFileTextAsync(_pathName);
                _model = JsonConvert.DeserializeObject<WbdkExportsModel>(json);
                if (_model == null) throw new InvalidWbdkExportsFileException(_pathName);
            }
        }

        public async Task<IEnumerable<string>> GetIncludeDependenciesAsync()
        {
            await EnsureModelLoadedAsync();
            return _model.DependentFiles ?? Constants.EmptyStringArray;
        }

        public async Task<IEnumerable<WbdkExportTableDependency>> GetTableDependenciesAsync()
        {
            await EnsureModelLoadedAsync();
            return _model.TableDependencies ?? WbdkExportTableDependency.EmptyArray;
        }
    }

    class WbdkExportsFileReaderFactory : IWbdkExportsFileReaderFactory
    {
        private DkAppContext _app;

        public WbdkExportsFileReaderFactory(DkAppContext app)
        {
            _app = app ?? throw new ArgumentNullException(nameof(app));
        }

        public IWbdkExportsFileReader CreateReader(string exportsPathName) => new WbdkExportsFileReader(_app, exportsPathName);
    }

    class InvalidWbdkExportsFileException : Exception
    {
        public InvalidWbdkExportsFileException(string pathName) : base($"Invalid WBDK exports file '{pathName}'.") { }
    }
}
