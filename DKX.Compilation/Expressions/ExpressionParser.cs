using DK.Code;
using DKX.Compilation.Conversions;
using DKX.Compilation.DataTypes;
using DKX.Compilation.Resolving;
using DKX.Compilation.Scopes;
using DKX.Compilation.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DKX.Compilation.Expressions
{
    static class ExpressionParser
    {
        public static async Task<Chain> TryReadExpressionAsync(Scope scope, DkxTokenStream stream, IResolver resolver)
        {
            var chain = await TryReadValueAsync(scope, stream, 0, resolver);
            if (chain == null) return null;
            return chain;
        }

        private static async Task<Chain> TryReadValueAsync(Scope scope, DkxTokenStream stream, OpPrec leftPrec, IResolver resolver)
        {
            var startPos = stream.Position;
            var token = stream.Read();
            if (token.Type == DkxTokenType.Keyword)
            {
                Chain chain;

                switch (token.Text)
                {
                    case DkxConst.Keywords.True:
                        chain = new BooleanLiteralChain(true, token.Span);
                        return await TryReadAfterValue(scope, stream, chain, leftPrec, resolver);
                    case DkxConst.Keywords.False:
                        chain = new BooleanLiteralChain(false, token.Span);
                        return await TryReadAfterValue(scope, stream, chain, leftPrec, resolver);

                    case DkxConst.Keywords.New:
                        var dtResult = await TryReadDataTypeAsync(scope, stream, resolver);
                        if (dtResult.Success)
                        {
                            chain = await ConstructorChain.ParseAsync(scope, dtResult.DataType, token.Span, dtResult.Span,
                                stream.Peek().IsBrackets ? stream.Read().Tokens : null, resolver);
                            return chain;
                        }
                        await scope.ReportAsync(token.Span, ErrorCode.ExpectedDataType);
                        return new ErrorChain(innerChainOrNull: null, token.Span);

                    default:
                        await scope.ReportAsync(token.Span, ErrorCode.SyntaxError);
                        return new ErrorChain(innerChainOrNull: null, token.Span);
                }
            }
            else if (token.IsIdentifier)
            {
                Chain chain;

                var variableStore = scope.GetScope<IVariableScope>()?.VariableStore;
                if (variableStore != null && variableStore.TryGetVariable(token.Text, includeParents: true, out var variable))
                {
                    // A variable defined with no context.
                    Chain thisChain;
                    if (variable.Local || variable.Static)
                    {
                        thisChain = null;
                    }
                    else
                    {
                        var objRefScope = scope.GetScope<IObjectReferenceScope>();
                        if (objRefScope == null) throw new InvalidOperationException("No object reference scope is available.");
                        if (objRefScope.ScopeStatic) await scope.ReportAsync(token.Span, ErrorCode.VariableRequiresThisPointer, variable.Name);
                        thisChain = new ThisChain(objRefScope.ScopeDataType, token.Span);
                    }

                    chain = new VariableChain(variable, token.Span, thisChain);
                    return await TryReadAfterValue(scope, stream, chain, leftPrec, resolver);
                }

                var constantStore = scope.GetScope<IConstantScope>()?.ConstantStore;
                if (constantStore != null && constantStore.TryGetConstant(token.Text, out var constant))
                {
                    chain = new ConstantChain(constant, token.Span);
                    return await TryReadAfterValue(scope, stream, chain, leftPrec, resolver);
                }

                await scope.ReportAsync(token.Span, ErrorCode.UnknownIdentifier, token.Text);
                return new ErrorChain(innerChainOrNull: null, token.Span);
            }
            else if (token.IsString)
            {
                if (token.HasError) await scope.ReportAsync(token.Span, ErrorCode.InvalidStringLiteral);
                var chain = new StringLiteralChain(token.Text, token.Span);
                return await TryReadAfterValue(scope, stream, chain, leftPrec, resolver);
            }
            else if (token.IsChar)
            {
                if (token.HasError) await scope.ReportAsync(token.Span, ErrorCode.InvalidCharLiteral);
                var chain = new CharLiteralChain(token.Char, token.Span);
                return await TryReadAfterValue(scope, stream, chain, leftPrec, resolver);
            }
            else if (token.IsNumber)
            {
                var chain = new NumericLiteralChain(token.Number, token.DataType, token.Span);
                return await TryReadAfterValue(scope, stream, chain, leftPrec, resolver);
            }
            else if (token.IsBrackets)
            {
                var subStream = new DkxTokenStream(token.Tokens);
                var chain = await TryReadValueAsync(scope, subStream, 0, resolver);
                if (chain == null)
                {
                    await scope.ReportAsync(token.Span, ErrorCode.ExpectedExpression);
                    return new ErrorChain(null, token.Span);
                }
                return await TryReadAfterValue(scope, stream, chain, leftPrec, resolver);
            }
            else
            {
                stream.Position = startPos;
                return null;
            }
        }

        private static async Task<Chain> TryReadAfterValue(Scope scope, DkxTokenStream stream, Chain chain, OpPrec leftPrec, IResolver resolver)
        {
            var startPos = stream.Position;
            var token = stream.Read();
            if (token.Type == DkxTokenType.Operator)
            {
                var op = token.Operator;

                if (op == Operator.Dot) return await ReadDotSequenceAsync(scope, stream, chain, leftPrec, resolver);

                var opPrec = op.GetPrecedence();
                if (leftPrec >= opPrec)
                {
                    // The left expression takes priority over the new operator,
                    // so don't consume the right operator at this time.
                    stream.Position = startPos;
                    return chain;
                }

                if (op.IsUnaryPost())
                {
                    var newChain = new OperatorChain(op, chain, right: null);
                    return await TryReadAfterValue(scope, stream, newChain, leftPrec, resolver);
                }
                else
                {
                    var right = await TryReadValueAsync(scope, stream, opPrec, resolver);
                    if (right == null)
                    {
                        // Was unable to read a value to the right of the operator, so stop the expression before this operator.
                        stream.Position = startPos;
                        await scope.ReportAsync(chain.Span.Envelope(token.Span), ErrorCode.OperatorExpectsValueOnRight, op.GetText());
                        return chain;
                    }
                    else
                    {
                        var newChain = new OperatorChain(op, chain, right);
                        return await TryReadAfterValue(scope, stream, newChain, leftPrec, resolver);
                    }
                }
            }
            else
            {
                stream.Position = startPos;
                return chain;
            }
        }

        private static async Task<Chain> ReadDotSequenceAsync(Scope scope, DkxTokenStream stream, Chain leftChain, OpPrec leftPrec, IResolver resolver)
        {
            // This method assumes the dot has just been read and the current token is the next token after the dot.

            var nameToken = stream.Read();
            if (nameToken.Type == DkxTokenType.Identifier)
            {
                if (stream.Peek().Type == DkxTokenType.Brackets)
                {
                    // Method call
                    var argsToken = stream.Read();
                    var args = await SplitArgumentExpressions(scope, argsToken.Tokens, nameToken.Span, resolver);

                    var methods = (await resolver.GetMethods(leftChain.DataType, nameToken.Text)).ToList();
                    var method = await FindBestMethodForArgumentsAsync(scope, methods, args, nameToken.Span);
                    if (method != null)
                    {
                        var chain = new MethodCallChain(leftChain, nameToken, args, argsToken.Span, method);
                        return await TryReadAfterValue(scope, stream, chain, leftPrec, resolver);
                    }
                    else return leftChain;
                }
                else
                {
                    // Property, field, or const
                    var fields = (await resolver.GetFields(leftChain.DataType, nameToken.Text)).ToList();
                    if (fields.Count == 0)
                    {
                        await scope.ReportAsync(nameToken.Span, ErrorCode.FieldNotFound, nameToken.Text);
                        return leftChain;
                    }
                    else if (fields.Count != 1)
                    {
                        await scope.ReportAsync(nameToken.Span, ErrorCode.AmbiguousField, nameToken.Text);
                        return leftChain;
                    }
                    else
                    {
                        var chain = new FieldChain(leftChain, nameToken, fields[0]);
                        return await TryReadAfterValue(scope, stream, chain, leftPrec, resolver);
                    }
                }
            }
            else
            {
                await scope.ReportAsync(nameToken.Span, ErrorCode.ExpectedMemberName);
                return leftChain;
            }
        }

        public static async Task<ReadDataTypeResult> TryReadDataTypeAsync(Scope scope, DkxTokenStream stream, IResolver resolver)
        {
            var startPos = stream.Position;
            var token = stream.Peek();
            if (token.Type == DkxTokenType.DataType)
            {
                stream.Position++;
                if (token.HasError) await scope.ReportAsync(token.Span, ErrorCode.InvalidDataType);
                return new ReadDataTypeResult(true, token.DataType, token.Span);
            }

            if (token.Type == DkxTokenType.Identifier)
            {
                stream.Position++;
                var name = token.Text;
                var class_ = await resolver.ResolveClassAsync(name);
                if (class_ != null) return new ReadDataTypeResult(true, new DataType(class_), token.Span);
            }

            stream.Position = startPos;
            return new ReadDataTypeResult(false, default, default);
        }

        public static async Task<Chain[]> SplitArgumentExpressions(Scope scope, DkxTokenCollection argBracketsTokens, CodeSpan errorSpan, IResolver resolver)
        {
            if (argBracketsTokens.Count == 0) return Chain.EmptyArray;

            var args = new List<Chain>();
            var reportedEmptyArg = false;
            foreach (var argTokens in argBracketsTokens.SplitByType(DkxTokenType.Delimiter))
            {
                if (argTokens.Count == 0)
                {
                    if (!reportedEmptyArg)
                    {
                        await scope.ReportAsync(errorSpan, ErrorCode.MethodContainsEmptyArguments);
                        reportedEmptyArg = true;
                    }
                }

                var argStream = new DkxTokenStream(argTokens);
                var expression = await TryReadExpressionAsync(scope, argStream, resolver);
                if (expression == null)
                {
                    await scope.ReportAsync(argTokens.Span, ErrorCode.ExpectedExpression);
                }
                args.Add(expression);

                if (!argStream.EndOfStream)
                {
                    await scope.ReportAsync(argStream.Read().Span, ErrorCode.SyntaxError);
                }
            }

            return args.ToArray();
        }

        public static async Task<IMethod> FindBestMethodForArgumentsAsync(Scope scope, IEnumerable<IMethod> methods, Chain[] args, CodeSpan errorSpan)
        {
            var methodsWithSameNumberOfArguments = methods.Where(m => m.Arguments.Length == args.Length).ToArray();
            if (methodsWithSameNumberOfArguments.Length == 0)
            {
                await scope.ReportAsync(errorSpan, ErrorCode.NoMethodWithSameNumberOfArguments);
                return null;
            }

            if (methodsWithSameNumberOfArguments.Length == 1) return methodsWithSameNumberOfArguments[0];

            var methodsAllGood = new List<IMethod>();
            var methodsWithWarnings = new List<IMethod>();
            foreach (var method in methodsWithSameNumberOfArguments)
            {
                var methodArgs = method.Arguments;
                var failed = false;
                var warning = false;
                for (var i = 0; i < methodArgs.Length; i++)
                {
                    switch (ConversionValidator.TestCompatibility(methodArgs[i].DataType, args[i].DataType, await args[i].GetConstantOrNullAsync(reportOrNull: null)))
                    {
                        case DataTypeCompatibility.Good:
                            break;
                        case DataTypeCompatibility.Warning:
                            warning = true;
                            break;
                        case DataTypeCompatibility.Fail:
                        case DataTypeCompatibility.ConstantOutOfRange:
                        default:
                            failed = true;
                            break;
                    }
                }

                if (failed) continue;
                if (warning) methodsWithWarnings.Add(method);
                else methodsAllGood.Add(method);
            }

            if (methodsAllGood.Count == 1)
            {
                return methodsAllGood[0];
            }
            else if (methodsAllGood.Count > 1)
            {
                await scope.ReportAsync(errorSpan, ErrorCode.MethodAmbiguous);
                return methodsAllGood[0];
            }
            else if (methodsWithWarnings.Count == 1)
            {
                return methodsAllGood[0];
            }
            else if (methodsWithWarnings.Count > 1)
            {
                await scope.ReportAsync(errorSpan, ErrorCode.MethodAmbiguous);
                return methodsWithWarnings[0];
            }
            else
            {
                await scope.ReportAsync(errorSpan, ErrorCode.NoMethodWithCompatibleArguments);
                return null;
            }
        }
    }

    struct ReadDataTypeResult
    {
        public bool Success { get; private set; }
        public DataType DataType { get; private set; }
        public CodeSpan Span { get; private set; }

        public ReadDataTypeResult(bool success, DataType dataType, CodeSpan span)
        {
            Success = success;
            DataType = dataType;
            Span = span;
        }
    }
}
