using DK.AppEnvironment;
using System;

namespace DKX.Compilation.ObjectFiles
{
    class ObjectFileReaderFactory : IObjectFileReaderFactory
    {
        private DkAppContext _app;

        public ObjectFileReaderFactory(DkAppContext app)
        {
            _app = app ?? throw new ArgumentNullException(nameof(app));
        }

        public IObjectFileReader CreateObjectFileReader(string objectPathName)
        {
            return new ObjectFileReader(_app, objectPathName);
        }
    }
}
