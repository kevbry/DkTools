using DKX.Compilation.Jobs;
using DKX.Compilation.Project;
using DKX.Compilation.ReportItems;
using DKX.Compilation.Resolving;
using DKX.Compilation.Scopes;
using System;
using System.Collections.Generic;

namespace DKX.Compilation.CodeGeneration
{
    class CodeGenerationContext
    {
        private CodeGenerationContext _parent;
        private FileTarget _fileTarget;
        private IReportItemCollector _report;
        private IProject _project;
        private IObjectReferenceScope _objRefScope;
        private HashSet<string> _fileDeps;
        private HashSet<string> _tableDeps;

        public CodeGenerationContext(FileTarget fileTarget, IReportItemCollector report, IProject project)
        {
            _parent = null;
            _fileTarget = fileTarget;
            _report = report ?? throw new ArgumentNullException(nameof(report));
            _project = project ?? throw new ArgumentNullException(nameof(project));
            _objRefScope = null;
        }

        public CodeGenerationContext(CodeGenerationContext parentContext, IObjectReferenceScope objRefScope)
        {
            _parent = parentContext ?? throw new ArgumentNullException(nameof(parentContext));
            _fileTarget = parentContext._fileTarget;
            _report = parentContext._report;
            _project = parentContext._project;
            _objRefScope = objRefScope ?? throw new ArgumentNullException(nameof(objRefScope));
        }

        public FileTarget FileTarget => _fileTarget;
        public IEnumerable<string> FileDependencies => (IEnumerable<string>)_fileDeps ?? DkxConst.EmptyStringArray;
        public IObjectReferenceScope ObjectReferenceScope => _objRefScope;
        public IProject Project => _project;
        public IReportItemCollector Report => _report;
        public IEnumerable<string> TableDependencies => (IEnumerable<string>)_tableDeps ?? DkxConst.EmptyStringArray;

        public void DependsOnFile(string pathName)
        {
            if (_parent != null)
            {
                _parent.DependsOnFile(pathName);
                return;
            }

            if (string.IsNullOrEmpty(pathName)) return;
            if (_fileDeps == null) _fileDeps = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            _fileDeps.Add(pathName);
        }

        public void DependsOnFile(Span span)
        {
            var pathName = span.PathName;
            if (string.IsNullOrEmpty(pathName)) return;
            if (pathName != null) DependsOnFile(pathName);
        }

        public void DependsOnTable(string tableName)
        {
            if (_parent != null)
            {
                _parent.DependsOnTable(tableName);
                return;
            }

            if (_tableDeps == null) _tableDeps = new HashSet<string>();
            if (!_tableDeps.Contains(tableName)) _tableDeps.Add(tableName);
        }

        public bool IsOutsideClass(IClass class_)
        {
            if (_objRefScope == null) throw new InvalidOperationException("No current object reference scope is defined.");

            var scopeDataType = _objRefScope.ScopeDataType;
            if (!scopeDataType.IsClass) return true;

            var scopeClass = _project.GetClassByFullNameOrNull(scopeDataType.ClassName);
            if (scopeClass == null) return true;

            if (scopeClass.FullClassName == class_.FullClassName) return false;
            if (class_.FullClassName.StartsWith(scopeClass.FullClassName + DkxConst.DelimiterToken)) return false;

            return true;
        }
    }
}
