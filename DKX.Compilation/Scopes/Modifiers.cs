using DK.Code;
using DKX.Compilation.Expressions;
using DKX.Compilation.ReportItems;
using DKX.Compilation.SystemClasses;
using DKX.Compilation.Tokens;
using DKX.Compilation.Variables.ConstTerms;
using System;
using System.Collections.Generic;
using System.Text;

namespace DKX.Compilation.Scopes
{
    class Modifiers
    {
        public Privacy? Privacy { get; private set; }
        public Span PrivacySpan { get; private set; }
        public FileContext? FileContext { get; private set; }
        public Span FileContextSpan { get; private set; }
        public bool Const { get; private set; }
        public Span ConstSpan { get; private set; }
        public ModifierFlags Flags { get; set; }
        public AttributeModifier[] Attributes { get; private set; }

        public Modifiers(
            Privacy? privacy, Span privacySpan,
            FileContext? fileContext, Span fileContextSpan,
            bool const_, Span constSpan,
            ModifierFlags flags,
            AttributeModifier[] attributes)
        {
            Privacy = privacy;
            PrivacySpan = privacySpan;
            FileContext = fileContext;
            FileContextSpan = fileContextSpan;
            Const = const_;
            ConstSpan = constSpan;
            Flags = flags;
            Attributes = attributes;
        }

        public bool IsEmpty => Privacy == null && FileContext == null && Const == false;

        public static Modifiers ReadModifiers(Scope scope, DkxTokenCollection tokens, int beforeIndex, TokenUseTracker used)
        {
            var privacy = null as Privacy?;
            var privacySpan = Span.Empty;
            var fileContext = null as FileContext?;
            var fileContextSpan = Span.Empty;
            var const_ = false;
            var constSpan = Span.Empty;
            ModifierFlags flags = default;

            var pos = beforeIndex - 1;
            for (var done = false; pos >= 0 && !done; pos--)
            {
                var token = tokens[pos];
                if (token.Type != DkxTokenType.Keyword) break;

                switch (token.Text)
                {
                    case DkxConst.Keywords.Public:
                        if (privacy != null) scope.Report(token.Span, ErrorCode.DuplicatePrivacyModifier);
                        privacy = Scopes.Privacy.Public;
                        privacySpan = token.Span;
                        used.Use(token);
                        break;
                    case DkxConst.Keywords.Protected:
                        if (privacy != null) scope.Report(token.Span, ErrorCode.DuplicatePrivacyModifier);
                        privacy = Scopes.Privacy.Protected;
                        privacySpan = token.Span;
                        used.Use(token);
                        break;
                    case DkxConst.Keywords.Private:
                        if (privacy != null) scope.Report(token.Span, ErrorCode.DuplicatePrivacyModifier);
                        privacy = Scopes.Privacy.Private;
                        privacySpan = token.Span;
                        used.Use(token);
                        break;
                    case DkxConst.Keywords.Neutral:
                        if (fileContext != null) scope.Report(token.Span, ErrorCode.DuplicateFileContextModifier);
                        fileContext = DK.Code.FileContext.NeutralClass;
                        fileContextSpan = token.Span;
                        used.Use(token);
                        break;
                    case DkxConst.Keywords.Client:
                        if (fileContext != null) scope.Report(token.Span, ErrorCode.DuplicateFileContextModifier);
                        fileContext = DK.Code.FileContext.ClientClass;
                        fileContextSpan = token.Span;
                        used.Use(token);
                        break;
                    case DkxConst.Keywords.Server:
                        if (fileContext != null) scope.Report(token.Span, ErrorCode.DuplicateFileContextModifier);
                        fileContext = DK.Code.FileContext.ServerClass;
                        fileContextSpan = token.Span;
                        used.Use(token);
                        break;
                    case DkxConst.Keywords.Const:
                        if (const_) scope.Report(token.Span, ErrorCode.DuplicateConstModifier);
                        const_ = true;
                        constSpan = token.Span;
                        used.Use(token);
                        break;
                    case DkxConst.Keywords.Static:
                        if (flags.HasFlag(ModifierFlags.Static)) scope.Report(token.Span, ErrorCode.DuplicateStaticModifier);
                        flags |= ModifierFlags.Static;
                        used.Use(token);
                        break;
                    default:
                        done = true;
                        pos++;
                        break;
                }
            }

            var attributes = new List<AttributeModifier>();

            while (pos >= 0 && tokens[pos].IsArray)
            {
                var arrToken = tokens[pos--];
                used.Use(arrToken);

                var stream = new DkxTokenStream(arrToken.Tokens);
                while (true)
                {
                    var token = stream.Peek();
                    if (!token.IsIdentifier())
                    {
                        scope.Report(token.Span, ErrorCode.SyntaxError);
                        break;
                    }

                    var attribToken = token;
                    stream.Position++;
                    if (stream.Peek().IsBrackets)
                    {
                        var attribArgsToken = stream.Read();
                        var attrib = ProcessAttribute(scope, attribToken, attribArgsToken);
                        if (attrib != null) attributes.Add(attrib);
                    }
                    else
                    {
                        attributes.Add(new AttributeModifier(attribToken.Text, ConstTerm.EmptyArray, attribToken.Span));
                    }

                    if (stream.EndOfStream) break;
                    if (stream.Peek().IsDelimiter)
                    {
                        stream.Position++;
                        continue;
                    }

                    scope.Report(token.Span, ErrorCode.SyntaxError);
                    break;
                }
            }

            return new Modifiers(privacy, privacySpan, fileContext, fileContextSpan, const_, constSpan, flags, attributes.ToArray());
        }

        private static AttributeModifier ProcessAttribute(Scope scope, DkxToken attribToken, DkxToken attribArgsToken)
        {
            if (attribArgsToken.Tokens.Count == 0) return new AttributeModifier(attribToken.Text, ConstTerm.EmptyArray, attribToken.Span);
            if (scope.Phase != CompilePhase.FullCompilation) return new AttributeModifier(attribToken.Text, ConstTerm.EmptyArray, attribToken.Span);

            var argsTokens = attribArgsToken.Tokens.SplitByType(DkxTokenType.Delimiter);
            var args = new List<ConstTerm>();

            foreach (var argTokens in argsTokens)
            {
                if (argTokens.Count == 0)
                {
                    scope.Report(attribToken.Span, ErrorCode.MethodContainsEmptyArguments);
                    return null;
                }

                var argStream = new DkxTokenStream(argTokens);
                var exp = ExpressionParser.ReadExpressionOrNull(scope, argStream, expectedDataType: default);
                if (exp == null)
                {
                    scope.Report((argStream.Position > 0 ? argStream.Peek(-1) : attribToken).Span, ErrorCode.MethodContainsEmptyArguments);
                    return null;
                }

                var constTerm = exp.ToConstTermOrNull(scope);
                if (constTerm == null)
                {
                    scope.Report(exp.Span, ErrorCode.ExpressionNotConstant);
                    return null;
                }

                args.Add(constTerm);
            }

            return new AttributeModifier(attribToken.Text, args.ToArray(), attribToken.Span + attribArgsToken.Span);
        }

        public void CheckForClass(IReportItemCollector report, Span classKeywordSpan) { }

        public void CheckForMethod(IReportItemCollector report, ClassScope class_, MethodScope method, CompilePhase phase)
        {
            if (Const) report.Report(ConstSpan, ErrorCode.InvalidConst);

            if (class_.Static && !Flags.IsStatic())
            {
                report.Report(method.NameSpan, ErrorCode.StaticClassesCannotHaveNonStaticMembers);
                return;
            }

            foreach (var attribute in Attributes)
            {
                SystemAttributes.CheckAttributeForMethod(attribute, this, method, phase);
            }
        }

        public void CheckForProperty(IReportItemCollector report, ClassScope class_, Span nameSpan)
        {
            if (Const) report.Report(ConstSpan, ErrorCode.InvalidConst);

            if (class_.Static && !Flags.IsStatic())
            {
                report.Report(nameSpan, ErrorCode.StaticClassesCannotHaveNonStaticMembers);
                return;
            }
        }

        public void CheckForPropertyAccessor(IReportItemCollector report, Modifiers propertyModifiers, Span keywordSpan)
        {
            if (Const)
            {
                report.Report(ConstSpan, ErrorCode.InvalidConst);
                return;
            }

            if (Flags.IsStatic())
            {
                report.Report(keywordSpan, ErrorCode.InvalidStatic);
                return;
            }

            if (FileContext != null)
            {
                switch (FileContext)
                {
                    case DK.Code.FileContext.ClientClass:
                    case DK.Code.FileContext.NeutralClass:
                    case DK.Code.FileContext.ServerClass:
                        break;
                    default:
                        report.Report(FileContextSpan, ErrorCode.InvalidFileContext);
                        break;
                }
            }

            if (Privacy != null && propertyModifiers.Privacy != null)
            {
                if (Privacy.Value < propertyModifiers.Privacy) report.Report(PrivacySpan, ErrorCode.PropertyAccessorMoreAccessibleThanProperty);
            }
        }

        public void CheckForMemberVariable(IReportItemCollector report, ClassScope class_, Span nameSpan)
        {
            if (Const)
            {
                report.Report(ConstSpan, ErrorCode.InvalidConst);
                return;
            }

            if (Privacy.HasValue && Privacy != Scopes.Privacy.Private)
            {
                report.Report(PrivacySpan, ErrorCode.MemberVariableMustBePrivate);
                return;
            }

            if (class_.Static && !Flags.IsStatic())
            {
                report.Report(nameSpan, ErrorCode.StaticClassesCannotHaveNonStaticMembers);
                return;
            }
        }

        public void CheckForConstant(IReportItemCollector report, Span nameSpan)
        {
            if (FileContext != null)
            {
                report.Report(FileContextSpan, ErrorCode.InvalidFileContext);
                return;
            }

            if (Flags.IsStatic())
            {
                report.Report(nameSpan, ErrorCode.InvalidStatic);
            }
        }

        public string ToSignature()
        {
            var sb = new StringBuilder();

            if (Privacy != null)
            {
                switch (Privacy.Value)
                {
                    case Scopes.Privacy.Public: sb.Append(DkxConst.Keywords.Public); break;
                    case Scopes.Privacy.Protected: sb.Append(DkxConst.Keywords.Protected); break;
                    case Scopes.Privacy.Private: sb.Append(DkxConst.Keywords.Private); break;
                }
            }

            if (Flags.HasFlag(ModifierFlags.Static))
            {
                if (sb.Length > 0) sb.Append(' ');
                sb.Append(DkxConst.Keywords.Static);
            }

            if (Const)
            {
                if (sb.Length > 0) sb.Append(' ');
                sb.Append(DkxConst.Keywords.Const);
            }

            if (FileContext != null)
            {
                if (sb.Length > 0) sb.Append(' ');
                if (FileContext.Value.IsClientSide()) sb.Append(DkxConst.Keywords.Client);
                else if (FileContext.Value.IsServerSide()) sb.Append(DkxConst.Keywords.Server);
                else sb.Append(DkxConst.Keywords.Neutral);
            }

            return sb.ToString();
        }

        public class AttributeModifier
        {
            public string Name { get; private set; }
            public ConstTerm[] Arguments { get; private set; }
            public Span Span { get; private set; }

            public AttributeModifier(string name, ConstTerm[] arguments, Span span)
            {
                Name = name ?? throw new ArgumentNullException(nameof(name));
                Arguments = arguments ?? throw new ArgumentNullException(nameof(arguments));
                Span = span;
            }
        }
    }
}
