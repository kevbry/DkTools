using DK.AppEnvironment;
using DK.Diagnostics;
using DKX.Compilation.Scopes;
using DKX.Compilation.Tokens;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DKX.Compilation.Resolving
{
    class ExportsProvider : IExportsProvider
    {
        private DkAppContext _app;
        private Dictionary<string, INamespaceExport> _namespaces = new Dictionary<string, INamespaceExport>();

        public ExportsProvider(DkAppContext app)
        {
            _app = app ?? throw new ArgumentNullException(nameof(app));
        }

        public async Task<INamespaceExport> GetNamespaceAsync(string name)
        {
            if (_namespaces.TryGetValue(name, out var ns)) return ns;

            ns = await FindNamespaceAsync(name);
            _namespaces[name] = ns; // Store it, even if it's null so we don't redo the searching each time.
            return ns;
        }

        private async Task<INamespaceExport> FindNamespaceAsync(string name)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));

            await _app.Log.DebugAsync("Finding namespace '{0}'.", name);

            foreach (var sourceDir in _app.Settings.SourceDirs)
            {
                if (string.IsNullOrEmpty(sourceDir)) continue;

                foreach (var dkxPathName in _app.FileSystem.GetFilesInDirectoryRecursive(sourceDir, "*" + DkxConst.DkxExtension))
                {
                    var source = await _app.FileSystem.ReadFileTextAsync(dkxPathName);
                    var cp = new DkxCodeParser(source);
                    var file = new FileScope(dkxPathName, cp, ProcessingDepth.ExportsOnly);

                    if (file.Namespace?.Name == name)
                    {
                        return file.Namespace;
                    }
                }
            }

            return null;
        }
    }
}
