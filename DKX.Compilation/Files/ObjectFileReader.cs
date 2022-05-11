using DK.AppEnvironment;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace DKX.Compilation.Files
{
    class ObjectFileReader : IObjectFileReader
    {
        private DkAppContext _app;
        private string _objPathName;
        private ObjectFileModel _model;

        public ObjectFileReader(DkAppContext app, string objPathName)
        {
            _app = app ?? throw new ArgumentNullException(nameof(app));
            _objPathName = objPathName ?? throw new ArgumentNullException(nameof(objPathName));
        }

        private void EnsureModelLoaded()
        {
            if (_model == null)
            {
                var json = _app.FileSystem.GetFileText(_objPathName);
                _model = JsonConvert.DeserializeObject<ObjectFileModel>(json);
                if (_model == null) throw new InvalidObjectFileException(_objPathName);
            }
        }

        public IEnumerable<ObjectFileDependency> GetFileDependencies()
        {
            EnsureModelLoaded();
            return _model.FileDependencies ?? ObjectFileDependency.EmptyArray;
        }

        public IEnumerable<ObjectTableDependency> GetTableDependencies()
        {
            EnsureModelLoaded();
            return _model.TableDependencies ?? ObjectTableDependency.EmptyArray;
        }

        public string GetWbdkPathName()
        {
            EnsureModelLoaded();
            return _model.DestinationPathName;
        }

        public string GetDkxPathName()
        {
            EnsureModelLoaded();
            return _model.SourcePathName;
        }
    }
}
