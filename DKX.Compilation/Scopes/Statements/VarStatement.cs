using DKX.Compilation.CodeGeneration;
using DKX.Compilation.Exceptions;
using DKX.Compilation.Expressions;
using DKX.Compilation.Objects;
using DKX.Compilation.Tokens;
using DKX.Compilation.Variables;
using System;

namespace DKX.Compilation.Scopes.Statements
{
    /// <summary>
    /// A variable declaration using the 'var' keyword.
    /// </summary>
    class VarStatement : Statement
    {
        private Variable _variable;
        private Chain _initializer;

        private VarStatement(Scope parent, Span keywordSpan)
            : base(parent, keywordSpan)
        {
        }

        public override bool IsEmpty => false;

        public static VarStatement Parse(Scope scope, DkxTokenCollection tokens)
        {
            var stream = new DkxTokenStream(tokens);
            var keywordToken = stream.Read();
            if (!keywordToken.IsKeyword(DkxConst.Keywords.Var)) throw new InvalidOperationException("Expected first token to be 'var'.");

            var varStatement = new VarStatement(scope, keywordToken.Span);

            var classScope = varStatement.GetScope<ClassScope>();
            var variableScope = varStatement.GetScope<IVariableScope>();
            var variableWbdkScope = varStatement.GetScope<IVariableWbdkScope>();

            try
            {
                if (stream.EndOfStream) throw new CodeException(keywordToken.Span, ErrorCode.ExpectedVariableName);
                var nameToken = stream.Read();
                if (!nameToken.IsIdentifier()) throw new CodeException(nameToken.Span, ErrorCode.ExpectedVariableName);
                var name = nameToken.Text;
                if (!Validation.VariableValidator.IsValidVariableName(name)) varStatement.Report(nameToken.Span, ErrorCode.InvalidVariableName, name);
                if (variableScope.VariableStore.HasVariable(name, includeParents: true, localOnly: true)) varStatement.Report(nameToken.Span, ErrorCode.DuplicateVariable, name);

                if (stream.EndOfStream) throw new CodeException(nameToken.Span, ErrorCode.VariableInitializationRequired);
                var assignToken = stream.Read();
                if (!assignToken.IsOperator(Operator.Assign)) throw new CodeException(assignToken.Span, ErrorCode.ExpectedToken, '=');

                var exp = ExpressionParser.ReadExpressionOrNull(scope, stream, expectedDataType: default);
                if (exp == null) throw new CodeException(assignToken.Span, ErrorCode.VariableInitializationRequired);
                var dataType = exp.InferredDataType;

                var variable = new Variable(
                    class_: classScope,
                    name: name,
                    wbdkName: variableWbdkScope.GetNewVariableWbdkName(name),
                    dataType: dataType,
                    fileContext: DK.Code.FileContext.NeutralClass,
                    passType: null,
                    accessMethod: FieldAccessMethod.Variable,
                    flags: default,
                    local: true,
                    privacy: Privacy.Public,
                    initializer: null,
                    span: nameToken.Span);

                variableScope.VariableStore.AddVariable(variable);
                variableWbdkScope.AddWbdkVariable(variable);

                var endToken = stream.Read();
                if (!endToken.IsStatementEnd) throw new CodeException(endToken.Span, ErrorCode.ExpectedStatementEndToken);

                if (!stream.EndOfStream) throw new CodeException(stream.Read().Span, ErrorCode.SyntaxError);

                varStatement._variable = variable;
                varStatement._initializer = exp;
            }
            catch (CodeException ex)
            {
                scope.AddReportItem(ex.ToReportItem());
            }

            return varStatement;
        }

        internal override void GenerateWbdkCode(CodeGenerationContext context, CodeWriter cw, FlowTrace flow)
        {
            if (_initializer == null) return;

            cw.Write(_variable.WbdkName);
            cw.WriteSpace();
            cw.Write(DkxConst.Operators.AssignChar);
            cw.WriteSpace();
            var frag = _initializer.ToWbdkCode_Read(context, flow);
            if (frag.DataType.IsClass) frag = ObjectAccess.GenerateInitializeToReference(frag);
            cw.Write(frag);
            cw.Write(DkxConst.StatementEndToken);
            cw.WriteLine();

            flow.OnVariableAssigned(_variable.WbdkName);
        }
    }
}
