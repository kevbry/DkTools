using DK.AppEnvironment;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DKX.Compilation.Files
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
