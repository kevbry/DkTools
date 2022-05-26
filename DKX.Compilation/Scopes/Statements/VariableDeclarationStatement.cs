using DK.Code;
using DKX.Compilation.CodeGeneration;
using DKX.Compilation.Conversions;
using DKX.Compilation.DataTypes;
using DKX.Compilation.Expressions;
using DKX.Compilation.Resolving;
using DKX.Compilation.Tokens;
using DKX.Compilation.Variables;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DKX.Compilation.Scopes.Statements
{
    class VariableDeclarationStatement : Statement
    {
        private DataType _dataType;
        private List<InitializationStatement> _initializers;

        private VariableDeclarationStatement(Scope parent, DataType dataType, CodeSpan dataTypeSpan)
            : base(parent, dataTypeSpan)
        {
            _dataType = dataType;
        }

        public override bool IsEmpty => _initializers == null;

        public static async Task<VariableDeclarationStatement> ParseAsync(Scope parent, DataType dataType, CodeSpan dataTypeSpan, DkxTokenStream stream, IResolver resolver)
        {
            var varDeclStmt = new VariableDeclarationStatement(parent, dataType, dataTypeSpan);

            var nameToken = stream.Read();
            if (!nameToken.IsIdentifier)
            {
                await varDeclStmt.ReportAsync(nameToken.Span, ErrorCode.ExpectedVariableName);
                return varDeclStmt;
            }

            while (true)
            {
                if (stream.Peek().IsOperator(Operator.Assign))
                {
                    var assignToken = stream.Read();
                    var initializerExp = await ExpressionParser.TryReadExpressionAsync(varDeclStmt, stream, resolver);
                    if (initializerExp == null)
                    {
                        await varDeclStmt.ReportAsync(assignToken.Span, ErrorCode.ExpectedExpression);
                    }
                    else
                    {
                        var variable = new Variable(
                            name: nameToken.Text,
                            wbdkName: nameToken.Text,
                            dataType: dataType,
                            fileContext: FileContext.NeutralClass,
                            passType: null,
                            static_: false,
                            local: true,
                            privacy: Privacy.Public,
                            initializer: null);

                        varDeclStmt.GetScope<IVariableScope>().VariableStore.AddVariable(variable);

                        if (varDeclStmt._initializers == null) varDeclStmt._initializers = new List<InitializationStatement>();
                        varDeclStmt._initializers.Add(new InitializationStatement { Variable = variable, Initializer = initializerExp });
                    }
                }
                else
                {
                    var variable = new Variable(
                        name: nameToken.Text,
                        wbdkName: nameToken.Text,
                        dataType: dataType,
                        fileContext: FileContext.NeutralClass,
                        passType: null,
                        static_: false,
                        local: true,
                        privacy: Privacy.Public,
                        initializer: null);

                    varDeclStmt.GetScope<IVariableScope>().VariableStore.AddVariable(variable);
                }

                var nextToken = stream.Peek();
                if (nextToken.IsStatementEnd) break;
                if (nextToken.IsDelimiter) continue;

                stream.Position++;
                await varDeclStmt.ReportAsync(nextToken.Span, ErrorCode.ExpectedToken, ';');
            }

            return varDeclStmt;
        }

        internal override async Task GenerateWbdkCodeAsync(CodeWriter cw)
        {
            if (_initializers == null) return;

            foreach (var stmt in _initializers)
            {
                cw.Write(stmt.Variable.WbdkName);
                cw.Write(" = ");
                var frag = await stmt.Initializer.ToWbdkCode_ReadAsync(this);
                await ConversionValidator.CheckConversionAsync(_dataType, frag, this);
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
