using DK.Code;
using DKX.Compilation.CodeGeneration;
using DKX.Compilation.Tokens;
using System;
using System.Collections.Generic;

namespace DKX.Compilation.Scopes
{
    public class NamespaceScope : Scope
    {
        private string _name;
        private Dictionary<string, ClassScope> _classes = new Dictionary<string, ClassScope>();

        public NamespaceScope(Scope parent, string name)
            : base(parent)
        {
            _name = name ?? throw new ArgumentNullException(nameof(name));
        }

        public IEnumerable<ClassScope> Classes => _classes.Values;
        public string Name => _name;

        public void ProcessTokens(DkxTokenCollection tokens, ProcessingDepth depth)
        {
            var used = new TokenUseTracker();

            foreach (var classIndex in tokens.FindIndices((t,i) =>
                t.Type == DkxTokenType.Keyword && t.Text == DkxConst.Keywords.Class &&
                tokens[i + 1].Type == DkxTokenType.Identifier &&
                tokens[i + 2].Type == DkxTokenType.Scope))
            {
                var classKeywordToken = tokens[classIndex];
                var classNameToken = tokens[classIndex + 1];
                var classScopeToken = tokens[classIndex + 2];
                used.Use(classKeywordToken, classNameToken, classScopeToken);

                var className = classNameToken.Text;

                var modifiers = Modifiers.ReadModifiers(tokens, classIndex, used, this);
                if (depth == ProcessingDepth.Full) modifiers.CheckForClass(this, classNameToken.Span);

                var class_ = new ClassScope(this, classNameToken.Text, modifiers);

                if (_classes.ContainsKey(className)) ReportItem(classNameToken.Span, ErrorCode.DuplicateClass, className);
                else _classes[className] = class_;

                class_.ProcessTokens(classScopeToken.Tokens, depth);
            }

            if (depth == ProcessingDepth.Full)
            {
                foreach (var badToken in tokens.GetUnused(used)) ReportItem(badToken.Span, ErrorCode.SyntaxError, badToken.ToString());
            }
        }

        public IEnumerable<FileContext> GetFileContexts()
        {
            var fileContexts = new List<FileContext>();
            foreach (var cls in _classes.Values)
            {
                foreach (var fc in cls.GetFileContexts())
                {
                    if (!fileContexts.Contains(fc)) fileContexts.Add(fc);
                }
            }
            return fileContexts;
        }

        internal override void GenerateWbdkCode(CodeWriter cw)
        {
            foreach (var cls in _classes.Values)
            {
                cls.GenerateWbdkCode(cw);
            }
        }
    }
}
