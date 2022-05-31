using DKX.Compilation.Conversions;
using DKX.Compilation.DataTypes;
using DKX.Compilation.Resolving;
using DKX.Compilation.Scopes;
using DKX.Compilation.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DKX.Compilation.Expressions
{
    static class ExpressionParser
    {
        public static Chain TryReadExpression(Scope scope, DkxTokenStream stream, IResolver resolver)
        {
            var chain = TryReadValue(scope, stream, 0, resolver);
            if (chain == null) return null;
            return chain;
        }

        private static Chain TryReadValue(Scope scope, DkxTokenStream stream, OpPrec leftPrec, IResolver resolver)
        {
            var startPos = stream.Position;

            if (TryReadDataType(scope, stream, resolver, out var dataType, out var dataTypeSpan))
            {
                var chain = new DataTypeChain(dataType, dataTypeSpan);
                return TryReadAfterValue(scope, stream, chain, leftPrec, resolver);
            }

            var token = stream.Read();
            if (token.Type == DkxTokenType.Keyword)
            {
                Chain chain;

                switch (token.Text)
                {
                    case DkxConst.Keywords.True:
                        chain = new BooleanLiteralChain(true, token.Span);
                        return TryReadAfterValue(scope, stream, chain, leftPrec, resolver);
                    case DkxConst.Keywords.False:
                        chain = new BooleanLiteralChain(false, token.Span);
                        return TryReadAfterValue(scope, stream, chain, leftPrec, resolver);

                    case DkxConst.Keywords.New:
                        if (TryReadDataType(scope, stream, resolver, out dataType, out dataTypeSpan))
                        {
                            chain = ConstructorChain.Parse(scope, dataType, token.Span, dataTypeSpan,
                                stream.Peek().IsBrackets ? stream.Read().Tokens : null, resolver);
                            return chain;
                        }
                        scope.Report(token.Span, ErrorCode.ExpectedDataType);
                        return new ErrorChain(innerChainOrNull: null, token.Span);

                    default:
                        scope.Report(token.Span, ErrorCode.SyntaxError);
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
                        if (objRefScope.ScopeStatic) scope.Report(token.Span, ErrorCode.VariableRequiresThisPointer, variable.Name);
                        thisChain = new ThisChain(objRefScope.ScopeDataType, token.Span);
                    }

                    chain = new VariableChain(variable, token.Span, thisChain);
                    return TryReadAfterValue(scope, stream, chain, leftPrec, resolver);
                }

                var constantStore = scope.GetScope<IConstantScope>()?.ConstantStore;
                if (constantStore != null && constantStore.TryGetConstant(token.Text, out var constant))
                {
                    chain = new ConstantChain(constant, token.Span);
                    return TryReadAfterValue(scope, stream, chain, leftPrec, resolver);
                }

                scope.Report(token.Span, ErrorCode.UnknownIdentifier, token.Text);
                return new ErrorChain(innerChainOrNull: null, token.Span);
            }
            else if (token.IsString)
            {
                if (token.HasError) scope.Report(token.Span, ErrorCode.InvalidStringLiteral);
                var chain = new StringLiteralChain(token.Text, token.Span);
                return TryReadAfterValue(scope, stream, chain, leftPrec, resolver);
            }
            else if (token.IsChar)
            {
                if (token.HasError) scope.Report(token.Span, ErrorCode.InvalidCharLiteral);
                var chain = new CharLiteralChain(token.Char, token.Span);
                return TryReadAfterValue(scope, stream, chain, leftPrec, resolver);
            }
            else if (token.IsNumber)
            {
                var chain = new NumericLiteralChain(token.Number, token.DataType, token.Span);
                return TryReadAfterValue(scope, stream, chain, leftPrec, resolver);
            }
            else if (token.IsBrackets)
            {
                var subStream = new DkxTokenStream(token.Tokens);
                var chain = TryReadValue(scope, subStream, 0, resolver);
                if (chain == null)
                {
                    scope.Report(token.Span, ErrorCode.ExpectedExpression);
                    return new ErrorChain(null, token.Span);
                }
                return TryReadAfterValue(scope, stream, chain, leftPrec, resolver);
            }
            else
            {
                stream.Position = startPos;
                return null;
            }
        }

        private static Chain TryReadAfterValue(Scope scope, DkxTokenStream stream, Chain chain, OpPrec leftPrec, IResolver resolver)
        {
            var startPos = stream.Position;
            var token = stream.Read();
            if (token.Type == DkxTokenType.Operator)
            {
                var op = token.Operator;

                if (op == Operator.Dot) return ReadDotSequence(scope, stream, chain, leftPrec, resolver);

                var opPrec = op.GetPrecedence();
                if ((leftPrec > opPrec) || (leftPrec == opPrec && op.IsLeftToRight()))
                {
                    // The left expression takes priority over the new operator,
                    // so don't consume the right operator at this time.
                    stream.Position = startPos;
                    return chain;
                }

                if (op.IsUnaryPost())
                {
                    var newChain = new OperatorChain(op, chain, right: null);
                    return TryReadAfterValue(scope, stream, newChain, leftPrec, resolver);
                }
                else
                {
                    var right = TryReadValue(scope, stream, opPrec, resolver);
                    if (right == null)
                    {
                        // Was unable to read a value to the right of the operator, so stop the expression before this operator.
                        stream.Position = startPos;
                        scope.Report(chain.Span.Envelope(token.Span), ErrorCode.OperatorExpectsValueOnRight, op.GetText());
                        return chain;
                    }
                    else
                    {
                        var newChain = new OperatorChain(op, chain, right);
                        return TryReadAfterValue(scope, stream, newChain, leftPrec, resolver);
                    }
                }
            }
            else
            {
                stream.Position = startPos;
                return chain;
            }
        }

        private static Chain ReadDotSequence(Scope scope, DkxTokenStream stream, Chain leftChain, OpPrec leftPrec, IResolver resolver)
        {
            // This method assumes the dot has just been read and the current token is the next token after the dot.

            var nameToken = stream.Read();
            if (nameToken.Type == DkxTokenType.Identifier)
            {
                if (stream.Peek().Type == DkxTokenType.Brackets)
                {
                    // Method call
                    var argsToken = stream.Read();
                    var args = SplitArgumentExpressions(scope, argsToken.Tokens, nameToken.Span, resolver);

                    var methods = resolver.GetMethods(leftChain.DataType, nameToken.Text).ToList();
                    var method = FindBestMethodForArguments(scope, methods, args, nameToken.Span);
                    if (method != null)
                    {
                        var chain = new MethodCallChain(leftChain, nameToken, args, argsToken.Span, method);
                        return TryReadAfterValue(scope, stream, chain, leftPrec, resolver);
                    }
                    else return leftChain;
                }
                else
                {
                    // Property, field, or const
                    var fields = resolver.GetFields(leftChain.DataType, nameToken.Text).ToList();
                    if (fields.Count == 0)
                    {
                        scope.Report(nameToken.Span, ErrorCode.FieldNotFound, nameToken.Text);
                        return leftChain;
                    }
                    else if (fields.Count != 1)
                    {
                        scope.Report(nameToken.Span, ErrorCode.AmbiguousField, nameToken.Text);
                        return leftChain;
                    }
                    else
                    {
                        var chain = new FieldChain(leftChain, nameToken, fields[0]);
                        return TryReadAfterValue(scope, stream, chain, leftPrec, resolver);
                    }
                }
            }
            else
            {
                scope.Report(nameToken.Span, ErrorCode.ExpectedMemberName);
                return leftChain;
            }
        }

        public static bool TryReadDataType(Scope scope, DkxTokenStream stream, IResolver resolver, out DataType dataTypeOut, out Span spanOut)
        {
            var startPos = stream.Position;
            var token = stream.Peek();
            if (token.Type == DkxTokenType.DataType)
            {
                stream.Position++;
                if (token.HasError) scope.Report(token.Span, ErrorCode.InvalidDataType);
                dataTypeOut = token.DataType;
                spanOut = token.Span;
                return true;
            }

            if (token.Type == DkxTokenType.Identifier)
            {
                stream.Position++;
                var name = token.Text;
                var class_ = resolver.ResolveClass(name);
                if (class_ != null)
                {
                    dataTypeOut = new DataType(class_);
                    spanOut = token.Span;
                    return true;
                }
            }

            stream.Position = startPos;
            dataTypeOut = default;
            spanOut = default;
            return false;
        }

        public static Chain[] SplitArgumentExpressions(Scope scope, DkxTokenCollection argBracketsTokens, Span errorSpan, IResolver resolver)
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
                        scope.Report(errorSpan, ErrorCode.MethodContainsEmptyArguments);
                        reportedEmptyArg = true;
                    }
                }

                var argStream = new DkxTokenStream(argTokens);
                var expression = TryReadExpression(scope, argStream, resolver);
                if (expression == null)
                {
                    scope.Report(argTokens.Span, ErrorCode.ExpectedExpression);
                }
                args.Add(expression);

                if (!argStream.EndOfStream)
                {
                    scope.Report(argStream.Read().Span, ErrorCode.SyntaxError);
                }
            }

            return args.ToArray();
        }

        public static IMethod FindBestMethodForArguments(Scope scope, IEnumerable<IMethod> methods, Chain[] args, Span errorSpan)
        {
            var methodsWithSameNumberOfArguments = methods.Where(m => m.Arguments.Length == args.Length).ToArray();
            if (methodsWithSameNumberOfArguments.Length == 0)
            {
                scope.Report(errorSpan, ErrorCode.NoMethodWithSameNumberOfArguments);
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
                    switch (ConversionValidator.TestCompatibility(methodArgs[i].DataType, args[i].DataType, srcConstant: null))
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
                scope.Report(errorSpan, ErrorCode.MethodAmbiguous);
                return methodsAllGood[0];
            }
            else if (methodsWithWarnings.Count == 1)
            {
                return methodsAllGood[0];
            }
            else if (methodsWithWarnings.Count > 1)
            {
                scope.Report(errorSpan, ErrorCode.MethodAmbiguous);
                return methodsWithWarnings[0];
            }
            else
            {
                scope.Report(errorSpan, ErrorCode.NoMethodWithCompatibleArguments);
                return null;
            }
        }
    }
}
