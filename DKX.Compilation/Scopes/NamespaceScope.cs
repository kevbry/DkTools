using DK;
using DK.AppEnvironment;
using DKX.Compilation.CodeGeneration;
using DKX.Compilation.Exceptions;
using DKX.Compilation.Resolving;
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

        public override string ToString() => $"NamespaceScope: {_name}";

        public void ProcessTokens(string namespaceName, DkxTokenCollection tokens)
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
                var fullClassName = string.Concat(namespaceName, DkxConst.Operators.Dot, className);
                var projectClass = Project.GetClassByFullNameOrNull(fullClassName);
                if (projectClass != null && !projectClass.DkxPathName.EqualsI(classNameToken.Span.PathName)) Report(classNameToken.Span, ErrorCode.DuplicateClass, fullClassName);

                var modifiers = Modifiers.ReadModifiers(this, tokens, classIndex, used);
                if (Phase == CompilePhase.FullCompilation) modifiers.CheckForClass(this, classNameToken.Span);

                var class_ = new ClassScope(this, namespaceName, className, fullClassName, GetScope<FileScope>().DkxPathName, classNameToken.Span, modifiers);

                if (_classes.ContainsKey(className)) Report(classNameToken.Span, ErrorCode.DuplicateClass, className);
                else _classes[className] = class_;

                if (Phase >= CompilePhase.MemberScan)
                {
                    class_.ProcessTokens(classScopeToken.Tokens);
                }
            }

            if (Phase == CompilePhase.FullCompilation)
            {
                foreach (var badToken in tokens.GetUnused(used)) Report(badToken.Span, ErrorCode.SyntaxError, badToken.ToString());
            }
        }

        public GeneratedCodeResult GenerateWbdkCode(string targetPath)
        {
            var results = new List<GeneratedCodeFile>();
            var fileDeps = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var tableDeps = new HashSet<string>();

            foreach (var cls in _classes.Values)
            {
                foreach (var fileTarget in cls.GetFileTargets())
                {
                    try
                    {
                        var context = new CodeGenerationContext(fileTarget, this, Project);
                        var cw = new CodeWriter();
                        cls.GenerateWbdkCode(context, cw);

                        var wbdkPathName = PathUtil.CombinePath(targetPath, fileTarget.RelativePathName);

                        results.Add(new GeneratedCodeFile(wbdkPathName, cw.Code, cls.FullClassName, cls.NamespaceName));

                        foreach (var fileDep in context.FileDependencies)
                        {
                            if (!fileDeps.Contains(fileDep)) fileDeps.Add(fileDep);
                        }
                        foreach (var tableDep in context.TableDependencies)
                        {
                            if (!tableDeps.Contains(tableDep)) tableDeps.Add(tableDep);
                        }
                    }
                    catch (CodeException ex)
                    {
                        Report(ex.Span, ex.ErrorCode, ex.Arguments);
                    }
                }
            }

            return new GeneratedCodeResult(results.ToArray(), fileDeps.ToArray(), tableDeps.ToArray());
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
