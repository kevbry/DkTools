using DK.AppEnvironment;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DKX.Compilation.ReportItems
{
    public class SourceCodeCache
    {
        private DkAppContext _app;
        private Dictionary<string, string> _files = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public SourceCodeCache(DkAppContext app)
        {
            _app = app ?? throw new ArgumentNullException(nameof(app));
        }

        public async Task<string> GetSourceCodeAsync(string pathName)
        {
            if (_files.TryGetValue(pathName, out var source)) return source;

            source = null;
            if (_app.FileSystem.FileExists(pathName))
            {
                source = await _app.FileSystem.ReadFileTextAsync(pathName);
            }

            _files[pathName] = source;
            return source;
        }
    }
}
