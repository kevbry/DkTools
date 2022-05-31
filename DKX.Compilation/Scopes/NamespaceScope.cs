using DK.AppEnvironment;
using DK.Code;
using DKX.Compilation.CodeGeneration;
using DKX.Compilation.Exceptions;
using DKX.Compilation.Resolving;
using DKX.Compilation.Tokens;
using System;
using System.Collections.Generic;

namespace DKX.Compilation.Scopes
{
    class NamespaceScope : Scope, INamespace
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
        public string NamespaceName => _name;

        public void ProcessTokens(string namespaceName, DkxTokenCollection tokens, ProcessingDepth depth, IResolver globalResolver)
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

                var modifiers = Modifiers.ReadModifiers(tokens, classIndex, used, this);
                if (depth == ProcessingDepth.Full) modifiers.CheckForClass(this, classNameToken.Span);

                var class_ = new ClassScope(this, namespaceName, classNameToken.Text, GetScope<FileScope>().DkxPathName, modifiers);

                if (_classes.ContainsKey(className)) Report(classNameToken.Span, ErrorCode.DuplicateClass, className);
                else _classes[className] = class_;

                class_.ProcessTokens(classScopeToken.Tokens, depth, namespaceResolver);
            }

            if (depth == ProcessingDepth.Full)
            {
                foreach (var badToken in tokens.GetUnused(used)) Report(badToken.Span, ErrorCode.SyntaxError, badToken.ToString());
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

        public List<GeneratedCodeResult> GenerateWbdkCode(string targetPath)
        {
            var results = new List<GeneratedCodeResult>();

            var context = new CodeGenerationContext(this);

            foreach (var cls in _classes.Values)
            {
                foreach (var fileContext in cls.GetFileContexts())
                {
                    try
                    {
                        var cw = new CodeWriter();
                        cls.GenerateWbdkCode(context, cw);

                        var wbdkFileName = cls.WbdkClassName + fileContext.GetExtension();
                        var wbdkPathName = PathUtil.CombinePath(targetPath, wbdkFileName);

                        results.Add(new GeneratedCodeResult(wbdkPathName, cw.Code, cls.FullClassName, cls.NamespaceName));
                    }
                    catch (CodeException ex)
                    {
                        Report(ex.Span, ex.ErrorCode, ex.Arguments);
                    }
                }
            }

            return results;
        }

        internal override void GenerateWbdkCode(CodeGenerationContext context, CodeWriter cw)
        {
            // This will never be called for namespaces.
            throw new NotImplementedException();
        }

        IEnumerable<IClass> INamespace.Classes => _classes.Values;

        public ClassScope GetClass(string name)
        {
            if (_classes.TryGetValue(name, out var class_)) return class_;
            return null;
        }

        IClass INamespace.GetClass(string name)
        {
            if (_classes.TryGetValue(name, out var class_)) return class_;
            return null;
        }
    }
}
