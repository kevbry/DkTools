using DK.AppEnvironment;
using DK.Code;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DKX.Compilation.ObjectFiles
{
    class ObjectFileReader : IObjectFileReader
    {
        private DkAppContext _app;
        private string _objectPathName;
        private ObjectFileModel _model;
        private Dictionary<string, string> _tableHashes;

        public ObjectFileReader(DkAppContext app, string objectPathName)
        {
            _app = app ?? throw new ArgumentNullException(nameof(app));
            _objectPathName = objectPathName ?? throw new ArgumentNullException(nameof(objectPathName));
        }

        private async Task CheckModelAsync()
        {
            if (_model == null)
            {
                var json = await _app.FileSystem.ReadFileTextAsync(_objectPathName);
                _model = JsonConvert.DeserializeObject<ObjectFileModel>(json);
            }
        }

        public async Task<IEnumerable<string>> GetFileDependenciesAsync()
        {
            await CheckModelAsync();
            return _model.FileDependencies?.Select(x => x.PathName).ToArray() ?? DkxConst.EmptyStringArray;
        }

        public async Task<IDictionary<string, string>> GetTableDependenciesAsync()
        {
            await CheckModelAsync();
            if (_tableHashes == null)
            {
                _tableHashes = new Dictionary<string, string>();
                if (_model.TableDependencies != null)
                {
                    foreach (var td in _model.TableDependencies)
                    {
                        _tableHashes[td.TableName] = td.Hash;
                    }
                }
            }
            return _tableHashes;
        }

        public async Task<IEnumerable<FileContext>> GetFileContextsAsync()
        {
            await CheckModelAsync();
            return _model.FileContexts?.Select(x => x.Context) ?? FileContextHelper.EmptyArray;
        }
    }
}
