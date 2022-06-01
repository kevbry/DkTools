using DK.Code;
using DKX.Compilation.CodeGeneration;
using DKX.Compilation.Conversions;
using DKX.Compilation.DataTypes;
using DKX.Compilation.Expressions;
using DKX.Compilation.Resolving;
using DKX.Compilation.Tokens;
using DKX.Compilation.Variables;
using System.Collections.Generic;

namespace DKX.Compilation.Scopes.Statements
{
    class VariableDeclarationStatement : Statement
    {
        private DataType _dataType;
        private List<InitializationStatement> _initializers;

        private VariableDeclarationStatement(Scope parent, DataType dataType, Span dataTypeSpan)
            : base(parent, dataTypeSpan)
        {
            _dataType = dataType;
        }

        public override bool IsEmpty => _initializers == null;

        public static VariableDeclarationStatement Parse(Scope parent, DataType dataType, Span dataTypeSpan, DkxTokenStream stream, IResolver resolver)
        {
            var varDeclStmt = new VariableDeclarationStatement(parent, dataType, dataTypeSpan);

            var nameToken = stream.Read();
            if (!nameToken.IsIdentifier)
            {
                varDeclStmt.Report(nameToken.Span, ErrorCode.ExpectedVariableName);
                return varDeclStmt;
            }

            while (true)
            {
                if (stream.Peek().IsOperator(Operator.Assign))
                {
                    var assignToken = stream.Read();
                    var initializerExp = ExpressionParser.TryReadExpression(varDeclStmt, stream);
                    if (initializerExp == null)
                    {
                        varDeclStmt.Report(assignToken.Span, ErrorCode.ExpectedExpression);
                    }
                    else
                    {
                        var variable = new Variable(
                            class_: parent.GetScope<IClass>(),
                            name: nameToken.Text,
                            wbdkName: nameToken.Text,
                            dataType: dataType,
                            fileContext: FileContext.NeutralClass,
                            passType: null,
                            accessMethod: FieldAccessMethod.Variable,
                            static_: false,
                            local: true,
                            privacy: Privacy.Public,
                            initializer: null,
                            span: nameToken.Span);

                        varDeclStmt.GetScope<IVariableScope>().VariableStore.AddVariable(variable);

                        if (varDeclStmt._initializers == null) varDeclStmt._initializers = new List<InitializationStatement>();
                        varDeclStmt._initializers.Add(new InitializationStatement { Variable = variable, Initializer = initializerExp });
                    }
                }
                else
                {
                    var variable = new Variable(
                        class_: parent.GetScope<IClass>(),
                        name: nameToken.Text,
                        wbdkName: nameToken.Text,
                        dataType: dataType,
                        fileContext: FileContext.NeutralClass,
                        passType: null,
                        accessMethod: FieldAccessMethod.Variable,
                        static_: false,
                        local: true,
                        privacy: Privacy.Public,
                        initializer: null,
                        span: nameToken.Span);

                    varDeclStmt.GetScope<IVariableScope>().VariableStore.AddVariable(variable);
                }

                var nextToken = stream.Peek();
                if (nextToken.IsStatementEnd) break;
                if (nextToken.IsDelimiter) continue;

                stream.Position++;
                varDeclStmt.Report(nextToken.Span, ErrorCode.ExpectedToken, ';');
            }

            return varDeclStmt;
        }

        internal override void GenerateWbdkCode(CodeGenerationContext context, CodeWriter cw)
        {
            if (_initializers == null) return;

            foreach (var stmt in _initializers)
            {
                cw.Write(stmt.Variable.WbdkName);
                cw.Write(" = ");
                var frag = stmt.Initializer.ToWbdkCode_Read(context);
                ConversionValidator.CheckConversion(_dataType, frag, this);
                cw.Write(frag);
                cw.Write(DkxConst.StatementEndToken);
                cw.WriteLine();
            }
        }

        private struct InitializationStatement
        {
            public Variable Variable { get; set; }
            public Chain Initializer { get; set; }
        }
    }
}
