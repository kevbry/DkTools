using DK.Code;
using DKX.Compilation.CodeGeneration;
using DKX.Compilation.Project;
using DKX.Compilation.ReportItems;
using DKX.Compilation.Resolving;
using DKX.Compilation.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DKX.Compilation.Scopes
{
    class FileScope : Scope
    {
        private string _sourcePathName;
        private DkxCodeParser _cp;
        private List<ReportItem> _reportItems = new List<ReportItem>();
        private List<NamespaceScope> _namespaces = new List<NamespaceScope>();

        public FileScope(string sourcePathName, DkxCodeParser codeParser, CompilePhase phase, GlobalResolver resolver, IProject project)
            : base(parent: null, phase, resolver, project)
        {
            _sourcePathName = sourcePathName ?? throw new ArgumentNullException(nameof(sourcePathName));
            _cp = codeParser ?? throw new ArgumentNullException(nameof(codeParser));
        }

        public string DkxPathName => _sourcePathName;
        public IEnumerable<NamespaceScope> Namespaces => _namespaces;
        public IEnumerable<ReportItem> ReportItems => _reportItems;

        public void ProcessFile()
        {
            var fileTokens = _cp.ReadAll().Tokens;
            var stream = new DkxTokenStream(fileTokens);
            var resolver = new GlobalResolver(Project, DkxConst.EmptyStringArray);

            while (!stream.EndOfStream)
            {
                var token = stream.Read();
                if (token.IsKeyword(DkxConst.Keywords.Namespace))
                {
                    var keywordToken = token;
                    var namespaceNameSB = new StringBuilder();
                    var gotErrors = false;

                    while (true)
                    {
                        token = stream.Read();
                        if (!token.IsIdentifier)
                        {
                            Report(token.Span, ErrorCode.ExpectedNamespaceName);
                            gotErrors = true;
                        }
                        else
                        {
                            if (namespaceNameSB.Length > 0) namespaceNameSB.Append('.');
                            namespaceNameSB.Append(token.Text);
                        }

                        token = stream.Peek();
                        if (token.IsScope) break;

                        stream.Position++;
                        if (token.IsOperator(Expressions.Operator.Dot)) continue;
                        Report(token.Span, ErrorCode.SyntaxError);
                        gotErrors = true;
                        break;
                    }

                    token = stream.Read();
                    if (token.IsScope)
                    {
                        if (!gotErrors)
                        {
                            var bodyToken = token;
                            var namespaceName = namespaceNameSB.ToString();

                            var namespace_ = _namespaces.Where(x => x.Name == namespaceName).FirstOrDefault();
                            if (namespace_ == null)
                            {
                                namespace_ = new NamespaceScope(this, namespaceName);
                                _namespaces.Add(namespace_);
                                resolver.AddUsingNamespace(namespaceName);
                            }

                            namespace_.ProcessTokens(namespaceName, bodyToken.Tokens, Phase, Resolver, Project);
                        }
                    }
                    else
                    {
                        Report(token.Span, ErrorCode.SyntaxError);
                        break;
                    }
                }
            }
        }

        public override void OnReport(ReportItem reportItem)
        {
            _reportItems.Add(reportItem);
        }

        public override bool HasErrors => _reportItems.Any(x => x.Severity == ErrorSeverity.Error);

        public GeneratedCodeResult GenerateWbdkCode(string targetPath)
        {
            var files = new List<GeneratedCodeFile>();
            var fileDeps = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var tableDeps = new HashSet<string>();

            foreach (var ns in _namespaces)
            {
                var result = ns.GenerateWbdkCode(targetPath);
                files.AddRange(result.GeneratedFiles);

                foreach (var fileDep in result.FileDependencies)
                {
                    if (!fileDeps.Contains(fileDep)) fileDeps.Add(fileDep);
                }

                foreach (var tableDep in result.TableDependencies)
                {
                    if (!tableDeps.Contains(tableDep)) tableDeps.Add(tableDep);
                }
            }

            fileDeps.Remove(_sourcePathName);   // Remove dependencies on itself

            return new GeneratedCodeResult(files.ToArray(), fileDeps.ToArray(), tableDeps.ToArray());
        }

        internal override void GenerateWbdkCode(CodeGenerationContext context, CodeWriter cw)
        {
            // This will never be called for file scope.
            throw new NotImplementedException();
        }

        public void ClearReportItems() => _reportItems.Clear();

        public NamespaceScope GetNamespaceOrNull(string name) => _namespaces.Where(x => x.Name == name).FirstOrDefault();
    }

    public class GeneratedCodeResult
    {
        public GeneratedCodeFile[] GeneratedFiles { get; private set; }
        public string[] FileDependencies { get; private set; }
        public string[] TableDependencies { get; private set; }

        public GeneratedCodeResult(GeneratedCodeFile[] files, string[] fileDeps, string[] tableDeps)
        {
            GeneratedFiles = files ?? throw new ArgumentNullException(nameof(files));
            FileDependencies = fileDeps ?? throw new ArgumentNullException(nameof(fileDeps));
            TableDependencies = tableDeps ?? throw new ArgumentNullException(nameof(tableDeps));
        }
    }

    public struct GeneratedCodeFile
    {
        public string WbdkPathName { get; private set; }
        public string Code { get; private set; }
        public string FullClassName { get; private set; }
        public string Namespace { get; private set; }

        public GeneratedCodeFile(string wbdkPathName, string code, string fullClassName, string namespaceName)
        {
            WbdkPathName = wbdkPathName ?? throw new ArgumentNullException(nameof(wbdkPathName));
            Code = code ?? throw new ArgumentNullException(nameof(code));
            FullClassName = fullClassName ?? throw new ArgumentNullException(nameof(fullClassName));
            Namespace = namespaceName ?? throw new ArgumentNullException(nameof(namespaceName));
        }
    }
}
