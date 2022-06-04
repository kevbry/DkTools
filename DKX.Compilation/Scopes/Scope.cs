using DKX.Compilation.CodeGeneration;
using DKX.Compilation.Objects;
using DKX.Compilation.Project;
using DKX.Compilation.ReportItems;
using DKX.Compilation.Resolving;
using DKX.Compilation.Tokens;
using System;
using System.Collections.Generic;

namespace DKX.Compilation.Scopes
{
    abstract class Scope : IReportItemCollector
    {
        private Scope _parent;
        private CompilePhase _phase;
        private IResolver _resolver;
        private IProject _project;

        public Scope(Scope parent, CompilePhase phase, IResolver resolver, IProject project)
        {
            _parent = parent;
            _phase = phase;
            _resolver = resolver;
            _project = project;
        }

        public Scope Parent => _parent;
        public CompilePhase Phase => _phase;
        public IProject Project => _project;
        public IResolver Resolver { get => _resolver; protected set => _resolver = value ?? throw new ArgumentNullException(); }

        public T GetScope<T>() where T : class => (this as T) ?? _parent?.GetScope<T>();

        public virtual void OnReport(ReportItem reportItem) => _parent.OnReport(reportItem);

        public virtual bool HasErrors => _parent.HasErrors;

        public void AddReportItem(ReportItem reportItem) => OnReport(reportItem);

        public void AddReportItems(IEnumerable<ReportItem> reportItems)
        {
            foreach (var reportItem in reportItems) OnReport(reportItem);
        }

        public void Report(Span span, ErrorCode code, params object[] args) => OnReport(new ReportItem(span, code, args));

        protected void ReportUnusedTokens(DkxTokenCollection tokens, TokenUseTracker used)
        {
            foreach (var badToken in tokens.GetUnused(used))
            {
                Report(badToken.Span, ErrorCode.SyntaxError);
            }
        }

        /// <summary>
        /// Generates code for leaving a scope.
        /// </summary>
        /// <param name="context">The current code generation context.</param>
        /// <param name="cw">The code writer.</param>
        /// <param name="flow">Flow trace for the current branch.</param>
        /// <param name="methodEnding">
        /// Set to true if the entire method is ending (e.g. return statement);
        /// or false if just leaving a { } scope (e.g. if statement body)
        /// </param>
        protected void GenerateScopeEnding(CodeGenerationContext context, CodeWriter cw, FlowTrace flow, bool methodEnding, Span span)
        {
            if (flow.IsEnded) return;

            if (methodEnding)
            {
                var variableWbdkScope = GetScope<IVariableWbdkScope>();
                foreach (var variable in variableWbdkScope.GetWbdkVariables())
                {
                    if (variable.DataType.BaseType != DataTypes.BaseType.Class) continue;

                    var variableFragment = new CodeFragment(variable.WbdkName, variable.DataType, Expressions.OpPrec.None, span, readOnly: false);
                    cw.Write(ObjectAccess.GenerateLeaveScope(variableFragment));
                    cw.Write(DkxConst.StatementEndToken);
                    cw.WriteLine();
                }
            }
            else
            {
                var variableScope = GetScope<IVariableScope>();
                foreach (var variable in variableScope.VariableStore.GetVariables(includeParents: false))
                {
                    if (!variable.Local) continue;
                    if (variable.DataType.BaseType != DataTypes.BaseType.Class) continue;

                    var variableFragment = new CodeFragment(variable.WbdkName, variable.DataType, Expressions.OpPrec.None, span, readOnly: false);
                    cw.Write(ObjectAccess.GenerateLeaveScope(variableFragment));
                    cw.Write(DkxConst.StatementEndToken);
                    cw.WriteLine();
                }
            }
        }
    }
}
