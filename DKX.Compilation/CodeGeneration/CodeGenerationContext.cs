using DKX.Compilation.Jobs;
using DKX.Compilation.Project;
using DKX.Compilation.ReportItems;
using System;
using System.Collections.Generic;

namespace DKX.Compilation.CodeGeneration
{
    class CodeGenerationContext
    {
        private FileTarget _fileTarget;
        private IReportItemCollector _report;
        private IProject _project;
        private HashSet<string> _fileDeps = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private HashSet<string> _tableDeps = new HashSet<string>();

        public CodeGenerationContext(FileTarget fileTarget, IReportItemCollector report, IProject project)
        {
            _fileTarget = fileTarget;
            _report = report ?? throw new ArgumentNullException(nameof(report));
            _project = project ?? throw new ArgumentNullException(nameof(project));
        }

        public FileTarget FileTarget => _fileTarget;
        public IEnumerable<string> FileDependencies => _fileDeps;
        public IProject Project => _project;
        public IReportItemCollector Report => _report;
        public IEnumerable<string> TableDependencies => _tableDeps;

        public void DependsOnFile(string pathName)
        {
            if (string.IsNullOrEmpty(pathName)) return;
            if (!_fileDeps.Contains(pathName)) _fileDeps.Add(pathName);
        }

        public void DependsOnFile(Span span)
        {
            var pathName = span.PathName;
            if (string.IsNullOrEmpty(pathName)) return;
            if (pathName != null) DependsOnFile(pathName);
        }

        public void DependsOnTable(string tableName)
        {
            if (!_tableDeps.Contains(tableName)) _tableDeps.Add(tableName);
        }
    }
}
