using DKX.Compilation.Project;
using DKX.Compilation.ReportItems;
using DKX.Compilation.Schema;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DKX.Compilation.CodeGeneration
{
    class CodeGenerationContext
    {
        private IReportItemCollector _report;
        private IProject _project;
        private HashSet<string> _fileDeps = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private HashSet<string> _tableDeps = new HashSet<string>();

        public CodeGenerationContext(IReportItemCollector report, IProject project)
        {
            _report = report ?? throw new ArgumentNullException(nameof(report));
            _project = project ?? throw new ArgumentNullException(nameof(project));
        }

        public IEnumerable<string> FileDependencies => _fileDeps;
        public IProject Project => _project;
        public IReportItemCollector Report => _report;
        public IEnumerable<string> TableDependencies => _tableDeps;

        public void DependsOnFile(string pathName)
        {
            if (!_fileDeps.Contains(pathName)) _fileDeps.Add(pathName);
        }

        public void DependsOnFile(Span span)
        {
            var pathName = span.PathName;
            if (pathName != null) DependsOnFile(pathName);
        }

        public void DependsOnTable(string tableName)
        {
            if (!_tableDeps.Contains(tableName)) _tableDeps.Add(tableName);
        }
    }
}
