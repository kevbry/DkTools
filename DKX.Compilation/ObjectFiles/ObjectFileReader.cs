using DK.AppEnvironment;
using DK.Code;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

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

        private void CheckModel()
        {
            if (_model == null)
            {
                _model = JsonConvert.DeserializeObject<ObjectFileModel>(_app.FileSystem.GetFileText(_objectPathName));
            }
        }

        public IEnumerable<string> GetFileDependencies()
        {
            CheckModel();
            return _model.FileDependencies?.Select(x => x.PathName).ToArray() ?? DkxConst.EmptyStringArray;
        }

        public IDictionary<string, string> GetTableDependencies()
        {
            CheckModel();
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

        public IEnumerable<FileContext> GetFileContexts()
        {
            CheckModel();
            return _model.FileContexts?.Select(x => x.Context) ?? FileContextHelper.EmptyArray;
        }
    }
}
