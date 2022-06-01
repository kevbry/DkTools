using DKX.Compilation.Conversions;
using DKX.Compilation.DataTypes;
using DKX.Compilation.Resolving;
using DKX.Compilation.Scopes;
using DKX.Compilation.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DKX.Compilation.Expressions
{
    static class ExpressionParser
    {
        public static Chain TryReadExpression(Scope scope, DkxTokenStream stream)
        {
            var chain = TryReadValue(scope, stream, 0);
            if (chain == null) return null;
            return chain;
        }

        private static Chain TryReadValue(Scope scope, DkxTokenStream stream, OpPrec leftPrec)
        {
            var startPos = stream.Position;

            if (TryReadDataType(scope, stream, out var dataType, out var dataTypeSpan))
            {
                var chain = new DataTypeChain(dataType, dataTypeSpan);
                return TryReadAfterValue(scope, stream, chain, leftPrec);
            }

            var token = stream.Read();
            if (token.Type == DkxTokenType.Keyword)
            {
                Chain chain;

                switch (token.Text)
                {
                    case DkxConst.Keywords.True:
                        chain = new BooleanLiteralChain(true, token.Span);
                        return TryReadAfterValue(scope, stream, chain, leftPrec);
                    case DkxConst.Keywords.False:
                        chain = new BooleanLiteralChain(false, token.Span);
                        return TryReadAfterValue(scope, stream, chain, leftPrec);

                    case DkxConst.Keywords.New:
                        if (TryReadDataType(scope, stream, out dataType, out dataTypeSpan))
                        {
                            chain = ConstructorChain.Parse(scope, dataType, token.Span, dataTypeSpan,
                                stream.Peek().IsBrackets ? stream.Read().Tokens : null);
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
                    return TryReadAfterValue(scope, stream, chain, leftPrec);
                }

                var constantStore = scope.GetScope<IConstantScope>()?.ConstantStore;
                if (constantStore != null && constantStore.TryGetConstant(token.Text, out var constant))
                {
                    chain = new ConstantChain(constant, token.Span);
                    return TryReadAfterValue(scope, stream, chain, leftPrec);
                }

                if (scope.Project.IsNamespaceStart(token.Text))
                {
                    return ReadNamespaceStart(scope, stream, token, leftPrec);
                }

                scope.Report(token.Span, ErrorCode.UnknownIdentifier, token.Text);
                return new ErrorChain(innerChainOrNull: null, token.Span);
            }
            else if (token.IsString)
            {
                if (token.HasError) scope.Report(token.Span, ErrorCode.InvalidStringLiteral);
                var chain = new StringLiteralChain(token.Text, token.Span);
                return TryReadAfterValue(scope, stream, chain, leftPrec);
            }
            else if (token.IsChar)
            {
                if (token.HasError) scope.Report(token.Span, ErrorCode.InvalidCharLiteral);
                var chain = new CharLiteralChain(token.Char, token.Span);
                return TryReadAfterValue(scope, stream, chain, leftPrec);
            }
            else if (token.IsNumber)
            {
                var chain = new NumericLiteralChain(token.Number, token.DataType, token.Span);
                return TryReadAfterValue(scope, stream, chain, leftPrec);
            }
            else if (token.IsBrackets)
            {
                var subStream = new DkxTokenStream(token.Tokens);
                var chain = TryReadValue(scope, subStream, 0);
                if (chain == null)
                {
                    scope.Report(token.Span, ErrorCode.ExpectedExpression);
                    return new ErrorChain(null, token.Span);
                }
                return TryReadAfterValue(scope, stream, chain, leftPrec);
            }
            else
            {
                stream.Position = startPos;
                return null;
            }
        }

        private static Chain TryReadAfterValue(Scope scope, DkxTokenStream stream, Chain chain, OpPrec leftPrec)
        {
            var startPos = stream.Position;
            var token = stream.Read();
            if (token.Type == DkxTokenType.Operator)
            {
                var op = token.Operator;

                if (op == Operator.Dot) return ReadDotSequence(scope, stream, chain, leftPrec);

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
                    return TryReadAfterValue(scope, stream, newChain, leftPrec);
                }
                else
                {
                    var right = TryReadValue(scope, stream, opPrec);
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
                        return TryReadAfterValue(scope, stream, newChain, leftPrec);
                    }
                }
            }
            else
            {
                stream.Position = startPos;
                return chain;
            }
        }

        private static Chain ReadDotSequence(Scope scope, DkxTokenStream stream, Chain leftChain, OpPrec leftPrec)
        {
            // This method assumes the dot has just been read and the current token is the next token after the dot.

            var nameToken = stream.Read();
            if (nameToken.Type == DkxTokenType.Identifier)
            {
                if (stream.Peek().Type == DkxTokenType.Brackets)
                {
                    // Method call
                    var argsToken = stream.Read();
                    var args = SplitArgumentExpressions(scope, argsToken.Tokens, nameToken.Span);

                    var methods = scope.Resolver.GetMethods(leftChain.DataType, nameToken.Text).ToList();
                    var method = FindBestMethodForArguments(scope, methods, args, nameToken.Span);
                    if (method != null)
                    {
                        var chain = new MethodCallChain(leftChain, nameToken, args, argsToken.Span, method);
                        return TryReadAfterValue(scope, stream, chain, leftPrec);
                    }
                    else return leftChain;
                }
                else
                {
                    // Property, field, or const
                    var fields = scope.Resolver.GetFields(leftChain.DataType, nameToken.Text).ToList();
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
                        return TryReadAfterValue(scope, stream, chain, leftPrec);
                    }
                }
            }
            else
            {
                scope.Report(nameToken.Span, ErrorCode.ExpectedMemberName);
                return leftChain;
            }
        }

        private static Chain ReadNamespaceStart(Scope scope, DkxTokenStream stream, DkxToken nsStartToken, OpPrec leftPrec)
        {
            // This method must return a Chain.

            var nsSpan = nsStartToken.Span;
            var sb = new StringBuilder(nsStartToken.Text);

            while (true)
            {
                if (!stream.Peek().IsOperator(Operator.Dot))
                {
                    scope.Report(nsSpan, ErrorCode.NamespaceNotValidHere, sb.ToString());
                    return new ErrorChain(innerChainOrNull: null, nsSpan);
                }
                stream.Position++;

                var token = stream.Read();
                if (!token.IsIdentifier)
                {
                    scope.Report(token.Span, ErrorCode.SyntaxError);
                    return new ErrorChain(innerChainOrNull: null, nsSpan + token.Span);
                }

                var ns = scope.Project.GetNamespaceOrNull(sb.ToString());
                if (ns == null) throw new InvalidOperationException("Namespace not found even though it was verified earlier.");

                var cls = ns.GetClass(token.Text);
                if (cls != null)
                {
                    var classChain = new DataTypeChain(new DataType(cls), nsSpan + token.Span);
                    if (stream.Peek().IsOperator(Operator.Dot))
                    {
                        stream.Position++;
                        return ReadDotSequence(scope, stream, classChain, leftPrec);
                    }
                    else
                    {
                        // Just a class name sitting by itself.
                        var chain = new DataTypeChain(new DataType(cls), nsSpan + token.Span);
                        return TryReadAfterValue(scope, stream, chain, leftPrec);
                    }
                }

                sb.Append(DkxConst.Operators.Dot);
                sb.Append(token.Text);

                if (scope.Project.IsNamespaceStart(sb.ToString()))
                {
                    // This is the next part of the namespace string.
                    nsSpan += token.Span;
                    continue;
                }
                else
                {
                    scope.Report(token.Span, ErrorCode.ClassNotFound, token.Text);
                    return new ErrorChain(innerChainOrNull: null, token.Span);
                }
            }
        }

        /// <summary>
        /// Attempts to read a data type token from the stream.
        /// If the token contains an error (invalid data type), it will be reported.
        /// </summary>
        /// <param name="scope">The scope we are currently under. Used to report errors.</param>
        /// <param name="stream">The stream to be read from.</param>
        /// <param name="dataTypeOut">(out) Resulting data type, if successful.</param>
        /// <param name="spanOut">(out) Span of the data type, if successful.</param>
        /// <returns>True if a data type could be read; otherwise false.</returns>
        public static bool TryReadDataType(Scope scope, DkxTokenStream stream, out DataType dataTypeOut, out Span spanOut)
        {
            var token = stream.Peek();

            if (TokenIsDataType(token, scope.Resolver, out dataTypeOut, out var isInvalid))
            {
                stream.Position++;
                if (isInvalid && scope.Phase == CompilePhase.FullCompilation) scope.Report(token.Span, ErrorCode.InvalidDataType);
                spanOut = token.Span;
                return true;
            }

            dataTypeOut = default;
            spanOut = default;
            return false;
        }

        /// <summary>
        /// Checks if the token is actually a data type, including class names.
        /// </summary>
        /// <param name="token">The token to be checked.</param>
        /// <param name="resolver">Resolver that will be used to attempt to find class references.</param>
        /// <param name="dataTypeOut">(out) If successful, the resulting data type.</param>
        /// <param name="isInvalidDataTypeOut">(out) If successful, and set to true, the caller should report ErrorCode.InvalidDataType.</param>
        /// <returns>True if the token is a data type; otherwise false.</returns>
        public static bool TokenIsDataType(DkxToken token, IResolver resolver, out DataType dataTypeOut, out bool isInvalidDataTypeOut)
        {
            if (token.Type == DkxTokenType.DataType)
            {
                dataTypeOut = token.DataType;
                isInvalidDataTypeOut = token.HasError;
                return true;
            }

            if (token.Type == DkxTokenType.Identifier)
            {
                var name = token.Text;
                var class_ = resolver.ResolveClass(name);
                if (class_ != null)
                {
                    dataTypeOut = new DataType(class_);
                    isInvalidDataTypeOut = false;
                    return true;
                }
            }

            dataTypeOut = default;
            isInvalidDataTypeOut = true;
            return false;
        }

        public static Chain[] SplitArgumentExpressions(Scope scope, DkxTokenCollection argBracketsTokens, Span errorSpan)
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
                var expression = TryReadExpression(scope, argStream);
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
