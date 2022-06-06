using DKX.Compilation.DataTypes;
using DKX.Compilation.Exceptions;
using DKX.Compilation.Resolving;
using DKX.Compilation.Scopes;
using DKX.Compilation.Tokens;
using DKX.Compilation.Validation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DKX.Compilation.Expressions
{
    static class ExpressionParser
    {
        public static Chain ReadExpressionOrNull(Scope scope, DkxTokenStream stream, DataType expectedDataType)
        {
            var chain = TryReadValue(scope, stream, 0, expectedDataType);
            if (chain == null) return null;
            return chain;
        }

        private static Chain TryReadValue(Scope scope, DkxTokenStream stream, OpPrec leftPrec, DataType expectedDataType)
        {
            var startPos = stream.Position;

            var token = stream.Read();
            if (token.Type == DkxTokenType.DataType)
            {
                if (token.HasError) scope.Report(token.Span, ErrorCode.InvalidDataType);
                var chain = new DataTypeChain(token.DataType, token.Span);
                return TryReadAfterValue(scope, stream, chain, leftPrec);
            }
            else if (token.Type == DkxTokenType.Keyword)
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
                        if (TryReadDataType(scope, stream, out var dataType, out var dataTypeSpan))
                        {
                            DkxToken argsToken;
                            if (stream.Peek().IsBrackets) argsToken = stream.Read();
                            else argsToken = default;

                            chain = ConstructorChain.Parse(scope, dataType, dataTypeSpan, argsToken);
                            return chain;
                        }
                        scope.Report(token.Span, ErrorCode.ExpectedDataType);
                        return new ErrorChain(innerChainOrNull: null, token.Span);

                    case DkxConst.Keywords.Null:
                        if (expectedDataType.IsClass)
                        {
                            chain = new TypedNullChain(expectedDataType, token.Span);
                            return TryReadAfterValue(scope, stream, chain, leftPrec);
                        }
                        else if (expectedDataType.IsValue) throw new CodeException(token.Span, ErrorCode.NullInvalidForDataType, expectedDataType);
                        else throw new CodeException(token.Span, ErrorCode.KeywordNotValidHere, expectedDataType);

                    case DkxConst.Keywords.This:
                        var objRefScope = scope.GetScope<IObjectReferenceScope>();
                        if (objRefScope.ScopeStatic) scope.Report(token.Span, ErrorCode.ThisRequiresObjectReference);
                        chain = new ThisChain(objRefScope.ScopeDataType, token.Span);
                        return TryReadAfterValue(scope, stream, chain, leftPrec);

                    default:
                        scope.Report(token.Span, ErrorCode.SyntaxError);
                        return new ErrorChain(innerChainOrNull: null, token.Span);
                }
            }
            else if (token.IsIdentifier())
            {
                if (stream.Peek().IsBrackets)
                {
                    // This is a method call

                    var methodNameToken = token;
                    var argsToken = stream.Read();
                    var args = SplitArgumentExpressions(scope, argsToken.Tokens, methodNameToken.Span);

                    var objRefScope = scope.GetScope<IObjectReferenceScope>();
                    var methods = scope.Resolver.GetMethods(objRefScope.ScopeDataType, methodNameToken.Text).ToList();
                    var method = FindBestMethodForArguments(methodNameToken.Text, methods, args, token.Span, isConstructor: false);

                    var thisChain = method.Flags.HasFlag(ModifierFlags.Static) ? (ThisChain)null : new ThisChain(objRefScope.ScopeDataType, methodNameToken.Span + argsToken.Span);
                    var chain = new MethodCallChain(thisChain, methodNameToken, args, argsToken.Span, method);
                    return TryReadAfterValue(scope, stream, chain, leftPrec);
                }
                else
                {
                    // This is a field

                    Chain chain;
                    Chain thisChain;
                    IObjectReferenceScope objRefScope = null;

                    var variableStore = scope.GetScope<IVariableScope>()?.VariableStore;
                    if (variableStore != null && variableStore.TryGetVariable(token.Text, includeParents: true, localOnly: false, out var variable))
                    {
                        // A variable used with no context.
                        if (variable.Local)
                        {
                            chain = new VariableChain(variable, token.Span);
                        }
                        else
                        {
                            if (objRefScope == null) objRefScope = scope.GetScope<IObjectReferenceScope>();
                            if (objRefScope.ScopeStatic && !variable.Flags.IsStatic()) scope.Report(token.Span, ErrorCode.VariableRequiresThisPointer, variable.Name);
                            thisChain = variable.Flags.IsStatic() ? (Chain)new DataTypeChain(objRefScope.ScopeDataType, token.Span) : new ThisChain(objRefScope.ScopeDataType, token.Span);
                            chain = new FieldChain(thisChain, token, variable);
                        }
                        
                        return TryReadAfterValue(scope, stream, chain, leftPrec);
                    }

                    if (objRefScope == null) objRefScope = scope.GetScope<IObjectReferenceScope>();
                    var fields = scope.Resolver.GetFields(objRefScope.ScopeDataType, token.Text).ToList();
                    if (fields.Count != 0)
                    {
                        if (fields.Count > 1) throw new CodeException(token.Span, ErrorCode.AmbiguousField, token.Text);
                        thisChain = fields[0].Flags.HasFlag(ModifierFlags.Static) ? null : new ThisChain(objRefScope.ScopeDataType, token.Span);
                        chain = new FieldChain(thisChain, token, fields[0]);
                        return TryReadAfterValue(scope, stream, chain, leftPrec);
                    }

                    var constantStore = scope.GetScope<IConstantScope>()?.ConstantStore;
                    if (constantStore != null && constantStore.TryGetConstant(token.Text, out var constant))
                    {
                        chain = new ConstantChain(constant, token.Span);
                        return TryReadAfterValue(scope, stream, chain, leftPrec);
                    }

                    var cls = scope.Resolver.ResolveClass(token.Text);
                    if (cls != null)
                    {
                        chain = new DataTypeChain(new DataType(cls), token.Span);
                        return TryReadAfterValue(scope, stream, chain, leftPrec);
                    }

                    if (scope.Project.IsNamespaceStart(token.Text))
                    {
                        return ReadNamespaceStart(scope, stream, token, leftPrec);
                    }

                    throw new CodeException(token.Span, ErrorCode.UnknownIdentifier, token.Text);
                }
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
            else if (token.IsOperator(Operator.Subtract) && stream.Peek().IsNumber)
            {
                token = stream.Read();
                var chain = new NumericLiteralChain(-token.Number, token.DataType, token.Span);
                return TryReadAfterValue(scope, stream, chain, leftPrec);
            }
            else if (token.IsBrackets)
            {
                var subStream = new DkxTokenStream(token.Tokens);
                var chain = TryReadValue(scope, subStream, 0, expectedDataType);
                if (chain == null)
                {
                    scope.Report(token.Span, ErrorCode.ExpectedExpression);
                    return new ErrorChain(null, token.Span);
                }
                if (!subStream.EndOfStream) scope.Report(subStream.Read().Span, ErrorCode.SyntaxError);
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
                    var right = TryReadValue(scope, stream, opPrec, chain.DataType);
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

        /// <summary>
        /// Reads an expression where a dot '.' is used to access a method/field of a value.
        /// </summary>
        /// <param name="scope">The scope the code lives in.</param>
        /// <param name="stream">The token stream being read from.</param>
        /// <param name="leftChain">A data type chain which kicked off this sequence.</param>
        /// <param name="leftPrec">Precedence of the operator on the left.</param>
        /// <returns>
        /// A chain for the method or field use.
        /// Always returns a value; throws on bad code.
        /// </returns>
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
                    var method = FindBestMethodForArguments(nameToken.Text, methods, args, nameToken.Span, isConstructor: false);

                    if (method.Flags.IsStatic() && !leftChain.IsStatic) throw new CodeException(nameToken.Span, ErrorCode.StaticMemberCannotHaveObjectReference, nameToken.Text);
                    if (!method.Flags.IsStatic() && leftChain.IsStatic) throw new CodeException(nameToken.Span, ErrorCode.MemberRequiresAnObjectReference, nameToken.Text);
                    var chain = new MethodCallChain(method.Flags.IsStatic() ? null : leftChain, nameToken, args, argsToken.Span, method);
                    return TryReadAfterValue(scope, stream, chain, leftPrec);
                }
                else
                {
                    // Property, field, or const
                    var fields = scope.Resolver.GetFields(leftChain.DataType, nameToken.Text).ToList();
                    if (fields.Count == 0) throw new CodeException(nameToken.Span, ErrorCode.FieldNotFound, nameToken.Text);
                    else if (fields.Count != 1) throw new CodeException(nameToken.Span, ErrorCode.AmbiguousField, nameToken.Text);
                    else
                    {
                        if (fields[0].Flags.IsStatic() && !leftChain.IsStatic) throw new CodeException(nameToken.Span, ErrorCode.StaticMemberCannotHaveObjectReference, nameToken.Text);
                        if (!fields[0].Flags.IsStatic() && leftChain.IsStatic) throw new CodeException(nameToken.Span, ErrorCode.MemberRequiresAnObjectReference, nameToken.Text);
                        var chain = new FieldChain(fields[0].Flags.IsStatic() ? null : leftChain, nameToken, fields[0]);
                        return TryReadAfterValue(scope, stream, chain, leftPrec);
                    }
                }
            }

            throw new CodeException(nameToken.Span, ErrorCode.ExpectedMemberName);
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
                if (!token.IsIdentifier())
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
            var resetPos = stream.Position;
            var token = stream.Peek();

            if (token.Type == DkxTokenType.DataType)
            {
                stream.Position++;
                dataTypeOut = token.DataType;
                if (token.HasError && scope.Phase == CompilePhase.FullCompilation) scope.Report(token.Span, ErrorCode.InvalidDataType);
                spanOut = token.Span;
                return true;
            }

            if (token.IsIdentifier())
            {
                var name = token.Text;
                var class_ = scope.Resolver.ResolveClass(name);
                if (class_ != null)
                {
                    stream.Position++;
                    dataTypeOut = new DataType(class_);
                    spanOut = token.Span;
                    return true;
                }

                if (scope.Project.IsNamespaceStart(name))
                {
                    stream.Position++;
                    var sb = new StringBuilder();
                    sb.Append(name);

                    while (true)
                    {
                        if (!stream.Read().IsOperator(Operator.Dot)) break;
                        token = stream.Read();
                        if (!token.IsIdentifier()) break;
                        name = token.Text;

                        var ns = scope.Project.GetNamespaceOrNull(sb.ToString());
                        var cls = ns?.GetClass(name);
                        if (cls != null)
                        {
                            dataTypeOut = new DataType(cls);
                            spanOut = stream[resetPos].Span + token.Span;
                            return true;
                        }

                        sb.Append(DkxConst.Operators.Dot);
                        sb.Append(name);
                    }

                    stream.Position = resetPos;
                }
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
                var expression = ReadExpressionOrNull(scope, argStream, expectedDataType: default);
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

        /// <summary>
        /// Picks the best method given a set of arguments.
        /// </summary>
        /// <param name="methods">A list of methods to choose from.</param>
        /// <param name="args">The arguments used to call the method.</param>
        /// <param name="methodNameSpan">Span of the method name (for error reporting)</param>
        /// <returns>
        /// The method with the best match.
        /// Throws an error if no good match could be found, or was ambiguous.
        /// </returns>
        public static IMethod FindBestMethodForArguments(string methodName, IEnumerable<IMethod> methods, Chain[] args, Span methodNameSpan, bool isConstructor)
        {
            if (!methods.Any()) throw new CodeException(methodNameSpan, ErrorCode.MethodNotFound, methodName);

            var methodsWithSameNumberOfArguments = methods.Where(m => m.Arguments.Length == args.Length).ToArray();
            if (methodsWithSameNumberOfArguments.Length == 0)
            {
                throw new CodeException(methodNameSpan, isConstructor
                    ? ErrorCode.NoConstructorWithSameNumberOfArguments
                    : ErrorCode.NoMethodWithSameNumberOfArguments, methodName);
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

            if (methodsAllGood.Count == 1) return methodsAllGood[0];
            if (methodsAllGood.Count > 1) throw new CodeException(methodNameSpan, isConstructor ? ErrorCode.ConstructorAmbiguous : ErrorCode.MethodAmbiguous, methodName);
            if (methodsWithWarnings.Count == 1) return methodsAllGood[0];
            if (methodsWithWarnings.Count > 1) throw new CodeException(methodNameSpan, isConstructor ? ErrorCode.ConstructorAmbiguous :  ErrorCode.MethodAmbiguous, methodName);
            throw new CodeException(methodNameSpan, isConstructor ? ErrorCode.NoConstructorWithCompatibleArguments : ErrorCode.NoMethodWithCompatibleArguments, methodName);
        }

        /// <summary>
        /// Converts a set of tokens into an expression.
        /// The expression is expected to consume the entire token collection.
        /// </summary>
        /// <param name="scope">The scope where the expression lives.</param>
        /// <param name="tokens">Tokens containing the expression.</param>
        /// <param name="errorSpan">A span preceding the expression, which can be used to report errors.</param>
        /// <returns>
        /// The resulting expression.
        /// Always returns a value; if bad code is detected, throws a CodeException.
        /// </returns>
        public static Chain TokensToExpressionStatement(Scope scope, DkxTokenCollection tokens, Span errorSpan)
        {
            if (tokens.Count == 0) throw new CodeException(errorSpan, ErrorCode.ExpectedExpression);

            var stream = new DkxTokenStream(tokens);
            var exp = ReadExpressionOrNull(scope, stream, expectedDataType: default);
            if (exp == null) throw new CodeException(errorSpan, ErrorCode.ExpectedExpression);

            if (!stream.EndOfStream) throw new CodeException(stream.Read().Span, ErrorCode.SyntaxError);

            return exp;
        }
    }
}
