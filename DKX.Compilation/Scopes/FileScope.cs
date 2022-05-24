using DK;
using DK.AppEnvironment;
using DK.Code;
using DKX.Compilation.CodeGeneration;
using DKX.Compilation.ObjectFiles;
using DKX.Compilation.ReportItems;
using DKX.Compilation.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DKX.Compilation.Scopes
{
    public class FileScope : Scope
    {
        private string _sourcePathName;
        private DkxCodeParser _cp;
        private List<ReportItem> _reportItems = new List<ReportItem>();
        private NamespaceScope _namespace;
        private ProcessingDepth _depth;

        public FileScope(string sourcePathName, DkxCodeParser codeParser, ProcessingDepth depth)
            : base(parent: null)
        {
            _sourcePathName = sourcePathName ?? throw new ArgumentNullException(nameof(sourcePathName));
            _cp = codeParser ?? throw new ArgumentNullException(nameof(codeParser));
            _depth = depth;
        }

        public IEnumerable<ReportItem> ReportItems => _reportItems;
        public NamespaceScope Namespace => _namespace;

        public void ProcessFile()
        {
            var fileTokens = _cp.ReadAll().Tokens;
            var used = new TokenUseTracker();

            foreach (var nsIndex in fileTokens.FindIndices((t,i) =>
                t.Type == DkxTokenType.Keyword && t.Text == DkxConst.Keywords.Namespace &&
                fileTokens[i + 1].Type == DkxTokenType.Identifier &&
                fileTokens[i + 2].Type == DkxTokenType.Scope))
            {
                var keywordToken = fileTokens[nsIndex];
                var nameToken = fileTokens[nsIndex + 1];
                var scopeToken = fileTokens[nsIndex + 2];
                used.Use(keywordToken, nameToken, scopeToken);

                var expectedNamespaceName = PathUtil.GetFileNameWithoutExtension(_sourcePathName);
                var namespaceName = nameToken.Text;
                if (namespaceName.EqualsI(expectedNamespaceName))
                {
                    if (namespaceName.Length <= DkxConst.Namespaces.MaxNamespaceLength)
                    {
                        if (_namespace == null) _namespace = new NamespaceScope(this, namespaceName);
                        _namespace.ProcessTokens(scopeToken.Tokens, _depth);
                    }
                    else
                    {
                        ReportItem(nameToken.Span, ErrorCode.NamespaceNameTooLong, DkxConst.Namespaces.MaxNamespaceLength);
                    }
                }
                else
                {
                    ReportItem(nameToken.Span, ErrorCode.NamespaceNameMustMatchFileName, expectedNamespaceName);
                }
            }

            if (_depth == ProcessingDepth.Full)
            {
                foreach (var badToken in fileTokens.GetUnused(used)) ReportItem(badToken.Span, ErrorCode.SyntaxError, badToken.ToString());
            }
        }

        public override void OnReport(CodeSpan span, ErrorCode errorCode, params object[] args)
        {
            if (_depth == ProcessingDepth.ExportsOnly) return;
            _reportItems.Add(new ReportItems.ReportItem(_sourcePathName, _cp.Source, span, errorCode, args));
        }

        public override bool HasErrors => _reportItems.Any(x => x.Severity == ErrorSeverity.Error);

        public string GenerateWbdkCode(FileContext fileContext)
        {
            var cw = new CodeWriter();
            GenerateWbdkCode(cw);
            return cw.Code;
        }

        internal override void GenerateWbdkCode(CodeWriter cw)
        {
            _namespace?.GenerateWbdkCode(cw);
        }

        public ObjectFileModel CreateObjectModel()
        {
            return new ObjectFileModel
            {
                FileDependencies = null,    // TODO
                TableDependencies = null,   // TODO
                FileContexts = _namespace?.GetFileContexts().Select(x => new ObjectFileContext { Context = x }).ToArray() ?? ObjectFileContext.EmptyArray
            };
        }
    }

    public enum ProcessingDepth
    {
        ExportsOnly,
        Full
    }
}
