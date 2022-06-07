using DK.AppEnvironment;
using DK.Diagnostics;
using DKX.Compilation.Project.Bson;
using DKX.Compilation.ReportItems;
using DKX.Compilation.Resolving;
using DKX.Compilation.SystemClasses;
using DKX.Compilation.Variables.ConstTerms;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DKX.Compilation.Project
{
    class DkxProject : IProject
    {
        private DkAppContext _app;
        private string _pathName;
        private Dictionary<string, ProjectFile> _files = new Dictionary<string, ProjectFile>(StringComparer.OrdinalIgnoreCase);
        private Dictionary<string, ProjectNamespace> _namespaces = new Dictionary<string, ProjectNamespace>();
        private CompilePhase _currentPhase;
        private DateTime _phaseStartTime;
        private SemaphoreSlim _fileScanSem = new SemaphoreSlim(1, 1);

        private DkxProject(DkAppContext app, string pathName)
        {
            _app = app ?? throw new ArgumentNullException(nameof(app));
            _pathName = pathName ?? throw new ArgumentNullException(nameof(pathName));
        }

        public static async Task<DkxProject> CreateAsync(DkAppContext app, string pathName)
        {
            var project = new DkxProject(app, pathName);
            await project.LoadAsync();
            return project;
        }

        public void OnCompilePhaseStarted(CompilePhase phase)
        {
            _currentPhase = phase;
            _phaseStartTime = DateTime.Now;
        }

        public async Task OnCompilePhaseCompleted(CompilePhase phase, IReportItemCollector report)
        {
            if (phase != _currentPhase) throw new InvalidOperationException($"Expected completion of phase '{_currentPhase}'.");

            switch (phase)
            {
                case CompilePhase.ClassScan:
                    await PurgeOldFilesAsync();
                    CheckForClassNameConflicts(report);
                    break;

                case CompilePhase.MemberScan:
                    break;

                case CompilePhase.ConstantResolution:
                    await ResolveAllConstantsAsync(report);
                    break;

                case CompilePhase.FullCompilation:
                    break;
            }
        }

        public async Task OnFileScanCompletedAsync(CompilePhase phase, string dkxPathName, IEnumerable<INamespace> namespaces)
        {
            await _fileScanSem.WaitAsync();
            try
            {
                if (phase == CompilePhase.FullCompilation)
                {
                    if (!_files.TryGetValue(dkxPathName, out var projectFile))
                    {
                        _files[dkxPathName] = projectFile = new ProjectFile(dkxPathName);
                    }

                    projectFile.SetCompileTime(DateTime.Now);
                }

                if (phase == CompilePhase.ClassScan || phase == CompilePhase.MemberScan || phase == CompilePhase.ConstantResolution)
                {
                    foreach (var fileNamespace in namespaces)
                    {
                        if (!_namespaces.TryGetValue(fileNamespace.NamespaceName, out var projectNamespace))
                        {
                            _namespaces[fileNamespace.NamespaceName] = projectNamespace = new ProjectNamespace(fileNamespace.NamespaceName, DateTime.Now);
                        }

                        projectNamespace.Update(phase, fileNamespace);
                    }
                }
            }
            finally
            {
                _fileScanSem.Release();
            }
        }

        public void OnCompileCompleted(string dkxPathName, IEnumerable<string> fileDependencies, IEnumerable<TableHash> tableDependencies)
        {
            if (!_files.TryGetValue(dkxPathName, out var projectFile))
            {
                _files[dkxPathName] = projectFile = new ProjectFile(dkxPathName);
            }

            projectFile.FileDependencies = fileDependencies;
            projectFile.TableDependencies = tableDependencies;
        }

        public DateTime GetCompileTimeStamp(string dkxPathName)
        {
            if (_files.TryGetValue(dkxPathName, out var file)) return file.GetCompileTime();
            return DateTime.MinValue;
        }

        public IEnumerable<string> GetFileDependencies(string dkxPathName)
        {
            if (_files.TryGetValue(dkxPathName, out var file)) return file.FileDependencies;
            return DkxConst.EmptyStringArray;
        }

        public IEnumerable<TableHash> GetTableDependencies(string dkxPathName)
        {
            if (_files.TryGetValue(dkxPathName, out var file)) return file.TableDependencies;
            return TableHash.EmptyArray;
        }

        public INamespace GetNamespaceOrNull(string namespaceName)
        {
            if (_namespaces.TryGetValue(namespaceName, out var ns)) return ns;
            return null;
        }

        public bool IsNamespaceStart(string name)
        {
            if (_namespaces.ContainsKey(name)) return true;

            var prefix = name + DkxConst.Operators.Dot;
            return _namespaces.Keys.Any(x => x.StartsWith(prefix));
        }

        public IClass GetClassByFullNameOrNull(string fullClassName)
        {
            var parts = fullClassName.Split('.');
            if (parts.Length < 2) throw new ArgumentException("A full class name should have 2 or more parts.");

            // TODO: Currently only supports root-level classes.
            // This will need to be enhanced later once nested classes are a thing.
            var className = parts[parts.Length - 1];
            var namespaceName = string.Join(".", parts, 0, parts.Length - 1);

            if (_namespaces.TryGetValue(namespaceName, out var ns)) return ns.GetClass(className);
            return null;
        }

        public async Task SaveAsync()
        {
            var data = SaveToBytes();
            await _app.FileSystem.WriteFileBytesAsync(_pathName, data);
        }

        private byte[] SaveToBytes()
        {
            var bson = new BsonFile();

            var bsonFiles = new BsonArray(bson);
            bson.Root.AddProperty("Files", bsonFiles);
            foreach (var file in _files.Values) bsonFiles.Add(file.ToBson(bson));

            var bsonNamespaces = new BsonArray(bson);
            bson.Root.AddProperty("Namespaces", bsonNamespaces);
            foreach (var ns in _namespaces.Values)
            {
                if (ns.System) continue;
                bsonNamespaces.Add(ns.ToBson(bson));
            }

            using (var memStream = new MemoryStream())
            {
                bson.Write(memStream);

                var length = memStream.Length;
                memStream.Seek(0, SeekOrigin.Begin);
                var data = new byte[length];
                memStream.Read(data, 0, (int)length);
                return data;
            }
        }

        public async Task LoadAsync()
        {
            _files.Clear();
            _namespaces.Clear();

            try
            {
                if (_app.FileSystem.FileExists(_pathName))
                {
                    var fileContent = await _app.FileSystem.ReadFileBytesAsync(_pathName);
                    Load(fileContent);
                }
            }
            catch (Exception ex)
            {
                await _app.Log.WarningAsync(ex, "Error when attempting to load DKX project file.");
                _files.Clear();
                _namespaces.Clear();
            }

            LoadSystemClasses();
        }

        private void Load(byte[] data)
        {
            using (var memStream = new MemoryStream(data))
            {
                var bson = new BsonFile();
                bson.Read(memStream);

                foreach (var file in bson.Root.GetArray("Files").Select(x => ProjectFile.FromBson(x)))
                {
                    _files[file.DkxPathName] = file;
                }

                foreach (var ns in bson.Root.GetArray("Namespaces").Select(x => ProjectNamespace.FromBson(x)))
                {
                    _namespaces[ns.NamespaceName] = ns;
                }
            }
        }

        private void LoadSystemClasses()
        {
            foreach (var sysClass in SystemClass.SystemClasses)
            {
                var nsName = sysClass.NamespaceName;
                if (_namespaces.TryGetValue(nsName, out var ns))
                {
                    if (!ns.System) throw new InvalidOperationException("System namespace is not system.");
                }
                else
                {
                    _namespaces[nsName] = ns = new ProjectNamespace(nsName, system: true);
                }

                var cls = new ProjectClass(sysClass.ClassName, sysClass.FullClassName, sysClass.NamespaceName, sysClass.WbdkClassName, sysClass.DkxPathName, Span.Empty);
                cls.Update(CompilePhase.FullCompilation, sysClass);
                ns.AddClass(cls);
            }
        }

        private async Task ResolveAllConstantsAsync(IReportItemCollector report)
        {
            await _app.Log.DebugAsync("Resolving constants");

            var context = new ConstResolutionContext(report, this);

            try
            {
                ClearAllConstants();

                var numUnresolved = CountUnresolvedConstants();
                await _app.Log.DebugAsync("Number of unresolved constants: {0}", numUnresolved);
                while (numUnresolved > 0)
                {
                    foreach (var ns in _namespaces.Values)
                    {
                        ns.ResolveAllConstants(context);
                    }

                    var nowUnresolved = CountUnresolvedConstants();
                    await _app.Log.DebugAsync("Number of unresolved constants: {0}", nowUnresolved);
                    if (nowUnresolved >= numUnresolved) throw new InvalidOperationException("The number of unresolved constants did not decrease in an iteration.");
                    numUnresolved = nowUnresolved;
                }
            }
            catch (CircularConstantDependencyException ex)
            {
                context.Report.Report(ex.Field.DefinitionSpan, ErrorCode.CircularConstantDependency, ex.Field.Name);
            }
        }

        private void ClearAllConstants()
        {
            foreach (var ns in _namespaces)
            {
                ns.Value.ClearAllConstants();
            }
        }

        private int CountUnresolvedConstants()
        {
            var count = 0;

            foreach (var ns in _namespaces.Values)
            {
                count += ns.CountUnresolvedConstants();
            }

            return count;
        }

        private async Task PurgeOldFilesAsync()
        {
            await _app.Log.DebugAsync("Purging old files from project.");

            var deletedFiles = new List<string>();
            foreach (var file in _files)
            {
                if (!_app.FileSystem.FileExists(file.Key))
                {
                    deletedFiles.Add(file.Key);
                }
            }

            foreach (var deletedFile in deletedFiles)
            {
                await _app.Log.DebugAsync("Purging file from project: {0}", deletedFile);
                _files.Remove(deletedFile);
            }
        }

        private void CheckForClassNameConflicts(IReportItemCollector report)
        {
            foreach (var ns in _namespaces.Values)
            {
                if (ns.System) continue;

                foreach (var cls in ns.Classes)
                {
                    if (_namespaces.TryGetValue(cls.FullClassName, out var conflictNS))
                    {
                        report.Report(cls.NameSpan, ErrorCode.ClassNameConflictsWithNamespace, cls.ClassName, conflictNS.NamespaceName);
                    }
                    else
                    {
                        var prefix = cls.FullClassName + DkxConst.Operators.Dot;
                        conflictNS = _namespaces.Values.Where(x => x.NamespaceName.StartsWith(prefix)).FirstOrDefault();
                        if (conflictNS != null)
                        {
                            report.Report(cls.NameSpan, ErrorCode.ClassNameConflictsWithNamespace, cls.ClassName, conflictNS.NamespaceName);
                        }
                    }
                }
            }
        }
    }

    class InvalidProjectFileException : Exception
    {
        public InvalidProjectFileException(string message) : base(message) { }
    }
}
