using DK.Code;
using DKX.Compilation.CodeGeneration;
using DKX.Compilation.DataTypes;
using DKX.Compilation.Exceptions;
using DKX.Compilation.Expressions;
using DKX.Compilation.Objects;
using DKX.Compilation.Resolving;
using DKX.Compilation.Tokens;
using DKX.Compilation.Validation;
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

        public static VariableDeclarationStatement Parse(Scope parent, DataType dataType, Span dataTypeSpan, DkxTokenStream stream)
        {
            var varDeclStmt = new VariableDeclarationStatement(parent, dataType, dataTypeSpan);
            var variableScope = varDeclStmt.GetScope<IVariableScope>();
            var variableWbdkScope = varDeclStmt.GetScope<IVariableWbdkScope>();

            try
            {
                while (true)
                {
                    var nameToken = stream.Read();
                    if (!nameToken.IsIdentifier()) throw new CodeException(nameToken.Span, ErrorCode.ExpectedVariableName);
                    var name = nameToken.Text;

                    if (!Validation.VariableValidator.IsValidVariableName(name)) varDeclStmt.Report(nameToken.Span, ErrorCode.InvalidVariableName, name);
                    if (variableScope.VariableStore.HasVariable(name, includeParents: true, localOnly: true)) varDeclStmt.Report(nameToken.Span, ErrorCode.DuplicateVariable, name);

                    if (stream.Peek().IsOperator(Operator.Assign))
                    {
                        var assignToken = stream.Read();
                        var initializerExp = ExpressionParser.ReadExpressionOrNull(varDeclStmt, stream, dataType);
                        if (initializerExp == null) throw new CodeException(assignToken.Span, ErrorCode.ExpectedExpression);

                        var variable = new Variable(
                            class_: parent.GetScope<IClass>(),
                            name: name,
                            wbdkName: variableWbdkScope.GetNewVariableWbdkName(name),
                            dataType: dataType,
                            fileContext: FileContext.NeutralClass,
                            passType: null,
                            accessMethod: FieldAccessMethod.Variable,
                            flags: default,
                            local: true,
                            privacy: Privacy.Public,
                            initializer: null,
                            span: nameToken.Span);

                        variableScope.VariableStore.AddVariable(variable);
                        variableWbdkScope.AddWbdkVariable(variable);

                        if (varDeclStmt._initializers == null) varDeclStmt._initializers = new List<InitializationStatement>();
                        varDeclStmt._initializers.Add(new InitializationStatement { Variable = variable, Initializer = initializerExp });
                    }
                    else
                    {
                        var variable = new Variable(
                            class_: parent.GetScope<IClass>(),
                            name: name,
                            wbdkName: variableWbdkScope.GetNewVariableWbdkName(name),
                            dataType: dataType,
                            fileContext: FileContext.NeutralClass,
                            passType: null,
                            accessMethod: FieldAccessMethod.Variable,
                            flags: default,
                            local: true,
                            privacy: Privacy.Public,
                            initializer: null,
                            span: nameToken.Span);

                        variableScope.VariableStore.AddVariable(variable);
                        variableWbdkScope.AddWbdkVariable(variable);

                        if (dataType.IsClass)
                        {
                            // Object reference variables should always be initialized to null, even when there's no explicit initializer.
                            if (varDeclStmt._initializers == null) varDeclStmt._initializers = new List<InitializationStatement>();
                            varDeclStmt._initializers.Add(new InitializationStatement
                            {
                                Variable = variable,
                                Initializer = new TypedNullChain(dataType, nameToken.Span)
                            });
                        }
                    }

                    var nextToken = stream.Peek();
                    if (nextToken.IsStatementEnd)
                    {
                        stream.Position++;
                        break;
                    }
                    if (nextToken.IsDelimiter)
                    {
                        stream.Position++;
                        continue;
                    }

                    stream.Position++;
                    throw new CodeException(nextToken.Span, ErrorCode.ExpectedToken, ';');
                }

                if (!stream.EndOfStream) throw new CodeException(stream.Read().Span, ErrorCode.SyntaxError);
            }
            catch (CodeException ex)
            {
                varDeclStmt.AddReportItem(ex.ToReportItem());
            }

            return varDeclStmt;
        }

        internal override void GenerateWbdkCode(CodeGenerationContext context, CodeWriter cw, FlowTrace flow)
        {
            if (_initializers == null) return;

            foreach (var stmt in _initializers)
            {
                cw.Write(stmt.Variable.WbdkName);
                cw.Write(" = ");
                var frag = stmt.Initializer.ToWbdkCode_Read(context, flow);
                if (frag.DataType.IsClass && !(stmt.Initializer is TypedNullChain)) frag = ObjectAccess.GenerateInitializeToReference(frag);
                ConversionValidator.CheckConversion(_dataType, frag, this);
                cw.Write(frag);
                cw.Write(DkxConst.StatementEndToken);
                cw.WriteLine();

                flow.OnVariableAssigned(stmt.Variable.WbdkName);
            }
        }

        private struct InitializationStatement
        {
            public Variable Variable { get; set; }
            public Chain Initializer { get; set; }
        }
    }
}
