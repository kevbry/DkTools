using DKX.Compilation.Nodes;
using DKX.Compilation.Tokens;
using System;
using System.Collections.Generic;

namespace DKX.Compilation.Scopes
{
    public class ClassScope : Scope
    {
        private string _name;
        private Modifiers _modifiers;
        private List<MethodScope> _methods = new List<MethodScope>();

        public ClassScope(Scope parent, string className, Modifiers modifiers)
            : base(parent)
        {
            _name = className ?? throw new ArgumentNullException(nameof(className));
            _modifiers = modifiers;
        }

        public IEnumerable<MethodScope> Methods => _methods;
        public string Name => _name;

        public void ProcessTokens(DkxTokenCollection tokens, ProcessingDepth depth)
        {
            var used = new TokenUseTracker();

            // Find methods
            foreach (var methodIndex in tokens.FindIndices((t,i) =>
                t.Type == DkxTokenType.DataType &&
                tokens[i + 1].Type == DkxTokenType.Identifier &&
                tokens[i + 2].Type == DkxTokenType.Arguments &&
                tokens[i + 3].Type == DkxTokenType.Scope))
            {
                var dataTypeToken = tokens[methodIndex];
                var nameToken = tokens[methodIndex + 1];
                var argumentsToken = tokens[methodIndex + 2];
                var scopeToken = tokens[methodIndex + 3];
                used.Use(dataTypeToken, nameToken, argumentsToken, scopeToken);

                var modifiers = Modifiers.ReadModifiers(tokens, methodIndex, used, this);
                modifiers.CheckForMethod(this);

                var method = new MethodScope(this, nameToken.Text, nameToken.Span, dataTypeToken.DataType, argumentsToken.Tokens, modifiers,
                    depth == ProcessingDepth.Full ? scopeToken.Tokens : null);
                _methods.Add(method);
            }

            foreach (var badToken in tokens.GetUnused(used)) ReportItem(badToken.Span, ErrorCode.SyntaxError);
        }
    }
}
