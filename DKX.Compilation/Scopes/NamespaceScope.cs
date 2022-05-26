using DK.Code;
using DKX.Compilation.CodeGeneration;
using DKX.Compilation.Resolving;
using DKX.Compilation.Tokens;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DKX.Compilation.Scopes
{
    class NamespaceScope : Scope, INamespaceExport, IClassNamingScope
    {
        private string _name;
        private Dictionary<string, ClassScope> _classes = new Dictionary<string, ClassScope>();

        public NamespaceScope(Scope parent, string name)
            : base(parent)
        {
            _name = name ?? throw new ArgumentNullException(nameof(name));
        }

        public IEnumerable<ClassScope> Classes => _classes.Values;
        public string FullClassName => _name;
        public IEnumerable<string> FullClassNameParts => new string[] { _name };
        public string Name => _name;

        public async Task ProcessTokens(DkxTokenCollection tokens, ProcessingDepth depth, IResolver globalResolver)
        {
            var used = new TokenUseTracker();
            var namespaceResolver = new NamespaceResolver(this, globalResolver);

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

                var modifiers = await Modifiers.ReadModifiersAsync(tokens, classIndex, used, this);
                if (depth == ProcessingDepth.Full) await modifiers.CheckForClassAsync(this, classNameToken.Span);

                var class_ = new ClassScope(this, classNameToken.Text, modifiers);

                if (_classes.ContainsKey(className)) await ReportAsync(classNameToken.Span, ErrorCode.DuplicateClass, className);
                else _classes[className] = class_;

                await class_.ProcessTokensAsync(classScopeToken.Tokens, depth, namespaceResolver);
            }

            if (depth == ProcessingDepth.Full)
            {
                foreach (var badToken in tokens.GetUnused(used)) await ReportAsync(badToken.Span, ErrorCode.SyntaxError, badToken.ToString());
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

        internal override async Task GenerateWbdkCodeAsync(CodeWriter cw)
        {
            foreach (var cls in _classes.Values)
            {
                await cls.GenerateWbdkCodeAsync(cw);
            }
        }

        IEnumerable<IClass> INamespaceExport.Classes => _classes.Values;

        public ClassScope GetClass(string name)
        {
            if (_classes.TryGetValue(name, out var class_)) return class_;
            return null;
        }

        IClass INamespaceExport.GetClass(string name)
        {
            if (_classes.TryGetValue(name, out var class_)) return class_;
            return null;
        }
    }
}
