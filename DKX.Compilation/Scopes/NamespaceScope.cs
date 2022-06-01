using DK.AppEnvironment;
using DK.Code;
using DKX.Compilation.CodeGeneration;
using DKX.Compilation.Exceptions;
using DKX.Compilation.Project;
using DKX.Compilation.Resolving;
using DKX.Compilation.Schema;
using DKX.Compilation.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DKX.Compilation.Scopes
{
    class NamespaceScope : Scope, INamespace
    {
        private string _name;
        private Dictionary<string, ClassScope> _classes = new Dictionary<string, ClassScope>();

        public NamespaceScope(Scope parent, string name)
            : base(parent, parent.Phase, parent.Resolver, parent.Project)
        {
            _name = name ?? throw new ArgumentNullException(nameof(name));

            Resolver = new NamespaceResolver(this, parent.Resolver);
        }

        public NamespaceAccessType AccessType => NamespaceAccessType.Normal;
        public IEnumerable<ClassScope> Classes => _classes.Values;
        public string Name => _name;
        public string NamespaceName => _name;

        public void ProcessTokens(string namespaceName, DkxTokenCollection tokens, CompilePhase phase, IResolver globalResolver, IProject project)
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
                if (phase == CompilePhase.FullCompilation) modifiers.CheckForClass(this, classNameToken.Span);

                var class_ = new ClassScope(this, namespaceName, classNameToken.Text, GetScope<FileScope>().DkxPathName, modifiers);

                if (_classes.ContainsKey(className)) Report(classNameToken.Span, ErrorCode.DuplicateClass, className);
                else _classes[className] = class_;

                if (phase >= CompilePhase.MemberScan)
                {
                    class_.ProcessTokens(classScopeToken.Tokens);
                }
            }

            if (phase == CompilePhase.FullCompilation)
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

        public GeneratedCodeResult GenerateWbdkCode(string targetPath)
        {
            var results = new List<GeneratedCodeFile>();
            var context = new CodeGenerationContext(this, Project);

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

                        results.Add(new GeneratedCodeFile(wbdkPathName, cw.Code, cls.FullClassName, cls.NamespaceName));
                    }
                    catch (CodeException ex)
                    {
                        Report(ex.Span, ex.ErrorCode, ex.Arguments);
                    }
                }
            }

            return new GeneratedCodeResult(results.ToArray(), context.FileDependencies.ToArray(), context.TableDependencies.ToArray());
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
