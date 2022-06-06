using DKX.Compilation.CodeGeneration;
using DKX.Compilation.DataTypes;
using DKX.Compilation.Exceptions;
using DKX.Compilation.Expressions;
using DKX.Compilation.Objects;
using DKX.Compilation.Tokens;
using System;

namespace DKX.Compilation.Scopes.Statements
{
    class ReturnStatement : Statement
    {
        private DataType _dataType;
        private Chain _expression;
        private bool _isConstructor;

        private ReturnStatement(Scope parent, Span keywordSpan) : base(parent, keywordSpan) { }

        public override bool IsEmpty => false;

        public static ReturnStatement Parse(Scope parent, DkxTokenCollection tokens)
        {
            if (tokens.Count == 0 || !tokens[0].IsKeyword(DkxConst.Keywords.Return)) throw new InvalidOperationException("Expected first token to be the 'return' keyword.");
            var keywordToken = tokens[0];

            var ret = new ReturnStatement(parent, keywordToken.Span);

            try
            {
                var returnScope = ret.GetScope<IReturnScope>();
                if (returnScope == null) throw new InvalidOperationException("Could not get return scope.");
                ret._dataType = returnScope.ReturnDataType;
                ret._isConstructor = returnScope.IsConstructor;

                if (ret._dataType.IsVoid || ret._isConstructor)
                {
                    if (tokens.Count == 1)
                    {
                        throw new CodeException(keywordToken.Span, ErrorCode.ExpectedToken, ';');
                    }
                    else
                    {
                        if (!tokens[1].IsStatementEnd) throw new CodeException(tokens[0].Span, ErrorCode.ExpectedToken, ';');
                        if (tokens.Count > 2) throw new CodeException(tokens[1].Span, ErrorCode.SyntaxError);
                    }
                }
                else
                {
                    var stream = new DkxTokenStream(tokens, 1);
                    var expression = ExpressionParser.ReadExpressionOrNull(ret, stream, ret._dataType);
                    if (expression == null) throw new CodeException(keywordToken.Span, ErrorCode.ExpectedExpression);

                    if (stream.EndOfStream || !stream.Peek().IsStatementEnd) throw new CodeException(stream.Read().Span, ErrorCode.ExpectedToken, ';');
                    stream.Position++;
                    if (!stream.EndOfStream) throw new CodeException(stream.Read().Span, ErrorCode.SyntaxError);
                    ret._expression = expression;
                }
            }
            catch (CodeException ex)
            {
                ret.AddReportItem(ex.ToReportItem());
            }
            
            return ret;
        }

        internal override void GenerateWbdkCode(CodeGenerationContext context, CodeWriter cw, FlowTrace flow)
        {
            GenerateScopeEnding(context, cw, flow, methodEnding: true, Span);

            cw.Write(DkxConst.Keywords.Return);
            if (_expression != null)
            {
                cw.WriteSpace();
                var frag = _expression.ToWbdkCode_Read(context, flow);
                if (frag.DataType.IsClass && !frag.IsUnownedObjectReference) frag = ObjectAccess.GenerateAddReference(frag);
                cw.Write(frag);
            }
            else if (_isConstructor)
            {
                cw.WriteSpace();
                cw.Write(DkxConst.Keywords.This);
            }
            cw.WriteStatementEnd();
            cw.WriteLine();

            flow.OnBranchEnded();
        }
    }
}
