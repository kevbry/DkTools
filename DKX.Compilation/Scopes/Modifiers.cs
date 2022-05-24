using DK.Code;
using DKX.Compilation.ReportItems;
using DKX.Compilation.Tokens;
using System.Text;

namespace DKX.Compilation.Scopes
{
    public struct Modifiers
    {
        public Privacy? Privacy { get; private set; }
        public CodeSpan PrivacySpan { get; private set; }
        public FileContext? FileContext { get; private set; }
        public CodeSpan FileContextSpan { get; private set; }
        public bool Const { get; private set; }
        public CodeSpan ConstSpan { get; private set; }
        public bool Static { get; private set; }
        public CodeSpan StaticSpan { get; private set; }

        public Modifiers(
            Privacy? privacy, CodeSpan privacySpan,
            FileContext? fileContext, CodeSpan fileContextSpan,
            bool const_, CodeSpan constSpan,
            bool static_, CodeSpan staticSpan)
        {
            Privacy = privacy;
            PrivacySpan = privacySpan;
            FileContext = fileContext;
            FileContextSpan = fileContextSpan;
            Const = const_;
            ConstSpan = constSpan;
            Static = static_;
            StaticSpan = staticSpan;
        }

        public bool IsEmpty => Privacy == null && FileContext == null && Const == false;

        public static Modifiers ReadModifiers(DkxTokenCollection tokens, int beforeIndex, TokenUseTracker used, ISourceCodeReporter report)
        {
            var privacy = null as Privacy?;
            var privacySpan = CodeSpan.Empty;
            var fileContext = null as FileContext?;
            var fileContextSpan = CodeSpan.Empty;
            var const_ = false;
            var constSpan = CodeSpan.Empty;
            var static_ = false;
            var staticSpan = CodeSpan.Empty;

            for (var pos = beforeIndex - 1; pos >= 0; pos--)
            {
                var token = tokens[pos];
                if (token.Type != DkxTokenType.Keyword) break;

                switch (token.Text)
                {
                    case DkxConst.Keywords.Public:
                        if (privacy != null) report.ReportItem(token.Span, ErrorCode.DuplicatePrivacyModifier);
                        privacy = Scopes.Privacy.Public;
                        privacySpan = token.Span;
                        used.Use(token);
                        break;
                    case DkxConst.Keywords.Protected:
                        if (privacy != null) report.ReportItem(token.Span, ErrorCode.DuplicatePrivacyModifier);
                        privacy = Scopes.Privacy.Protected;
                        privacySpan = token.Span;
                        used.Use(token);
                        break;
                    case DkxConst.Keywords.Private:
                        if (privacy != null) report.ReportItem(token.Span, ErrorCode.DuplicatePrivacyModifier);
                        privacy = Scopes.Privacy.Private;
                        privacySpan = token.Span;
                        used.Use(token);
                        break;
                    case DkxConst.Keywords.Neutral:
                        if (fileContext != null) report.ReportItem(token.Span, ErrorCode.DuplicateFileContextModifier);
                        fileContext = DK.Code.FileContext.NeutralClass;
                        fileContextSpan = token.Span;
                        used.Use(token);
                        break;
                    case DkxConst.Keywords.Client:
                        if (fileContext != null) report.ReportItem(token.Span, ErrorCode.DuplicateFileContextModifier);
                        fileContext = DK.Code.FileContext.ClientClass;
                        fileContextSpan = token.Span;
                        used.Use(token);
                        break;
                    case DkxConst.Keywords.Server:
                        if (fileContext != null) report.ReportItem(token.Span, ErrorCode.DuplicateFileContextModifier);
                        fileContext = DK.Code.FileContext.ServerClass;
                        fileContextSpan = token.Span;
                        used.Use(token);
                        break;
                    case DkxConst.Keywords.Const:
                        if (const_) report.ReportItem(token.Span, ErrorCode.DuplicateConstModifier);
                        const_ = true;
                        constSpan = token.Span;
                        used.Use(token);
                        break;
                    case DkxConst.Keywords.Static:
                        if (static_) report.ReportItem(token.Span, ErrorCode.DuplicateStaticModifier);
                        static_ = true;
                        staticSpan = token.Span;
                        used.Use(token);
                        break;
                    default:
                        pos = 0;
                        break;
                }
            }

            return new Modifiers(privacy, privacySpan, fileContext, fileContextSpan, const_, constSpan, static_, staticSpan);
        }

        public void CheckForClass(ISourceCodeReporter report, CodeSpan classKeywordSpan)
        {
        }

        public void CheckForMethod(ISourceCodeReporter report)
        {
            if (Const) report.ReportItem(ConstSpan, ErrorCode.InvalidConst);
        }

        public void CheckForProperty(ISourceCodeReporter report)
        {
            if (Const) report.ReportItem(ConstSpan, ErrorCode.InvalidConst);
        }

        public void CheckForPropertyAccessor(ISourceCodeReporter report, Modifiers propertyModifiers)
        {
            if (Const) report.ReportItem(ConstSpan, ErrorCode.InvalidConst);

            if (FileContext != null)
            {
                switch (FileContext)
                {
                    case DK.Code.FileContext.ClientClass:
                    case DK.Code.FileContext.NeutralClass:
                    case DK.Code.FileContext.ServerClass:
                        break;
                    default:
                        report.ReportItem(FileContextSpan, ErrorCode.InvalidFileContext);
                        break;
                }
            }

            if (Privacy != null && propertyModifiers.Privacy != null)
            {
                if (Privacy.Value < propertyModifiers.Privacy) report.ReportItem(PrivacySpan, ErrorCode.PropertyAccessorMoreAccessibleThanProperty);
            }
        }

        public void CheckForMemberVariable(ISourceCodeReporter report)
        {
            if (Const) report.ReportItem(ConstSpan, ErrorCode.InvalidConst);

            if (Privacy.HasValue && Privacy != Scopes.Privacy.Private) report.ReportItem(PrivacySpan, ErrorCode.MemberVariableMustBePrivate);
        }

        public void CheckForConstant(ISourceCodeReporter report)
        {
            if (FileContext != null) report.ReportItem(FileContextSpan, ErrorCode.InvalidFileContext);
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

            if (Static)
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
    }
}
