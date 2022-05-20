using DK.Code;
using DKX.Compilation.Files;
using DKX.Compilation.ReportItems;

namespace DKX.Compilation.Nodes
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

        public static Modifiers ReadModifiers(CodeParser code, ISourceCodeReporter report)
        {
            var privacy = null as Privacy?;
            var privacySpan = CodeSpan.Empty;
            var fileContext = null as FileContext?;
            var fileContextSpan = CodeSpan.Empty;
            var const_ = false;
            var constSpan = CodeSpan.Empty;
            var static_ = false;
            var staticSpan = CodeSpan.Empty;

            while (!code.EndOfFile)
            {
                switch (code.PeekWordR())
                {
                    case "public":
                        var wordSpan = code.MovePeekedSpan();
                        if (privacy != null) report.ReportItem(wordSpan, ErrorCode.DuplicatePrivacyModifier);
                        privacy = Files.Privacy.Public;
                        privacySpan = wordSpan;
                        continue;
                    case "protected":
                        wordSpan = code.MovePeekedSpan();
                        if (privacy != null) report.ReportItem(wordSpan, ErrorCode.DuplicatePrivacyModifier);
                        privacy = Files.Privacy.Protected;
                        privacySpan = wordSpan;
                        continue;
                    case "private":
                        wordSpan = code.MovePeekedSpan();
                        if (privacy != null) report.ReportItem(wordSpan, ErrorCode.DuplicatePrivacyModifier);
                        privacy = Files.Privacy.Private;
                        privacySpan = wordSpan;
                        continue;
                    case "neutral":
                        wordSpan = code.MovePeekedSpan();
                        if (fileContext != null) report.ReportItem(wordSpan, ErrorCode.DuplicateFileContextModifier);
                        fileContext = DK.Code.FileContext.NeutralClass;
                        fileContextSpan = wordSpan;
                        continue;
                    case "client":
                        wordSpan = code.MovePeekedSpan();
                        if (fileContext != null) report.ReportItem(wordSpan, ErrorCode.DuplicateFileContextModifier);
                        if (code.ReadExactWholeWord("trigger"))
                        {
                            fileContext = DK.Code.FileContext.ClientTrigger;
                            fileContextSpan = wordSpan.Envelope(code.Span);
                        }
                        else if (code.ReadExactWholeWord("class"))
                        {
                            fileContext = DK.Code.FileContext.ClientClass;
                            fileContextSpan = wordSpan.Envelope(code.Span);
                        }
                        else if (code.ReadExactWholeWord("program"))
                        {
                            fileContext = DK.Code.FileContext.GatewayProgram;
                            fileContextSpan = wordSpan.Envelope(code.Span);
                        }
                        else
                        {
                            fileContext = DK.Code.FileContext.ClientClass;
                            fileContextSpan = wordSpan;
                        }
                        continue;
                    case "server":
                        wordSpan = code.MovePeekedSpan();
                        if (fileContext != null) report.ReportItem(wordSpan, ErrorCode.DuplicateFileContextModifier);
                        if (code.ReadExactWholeWord("trigger"))
                        {
                            fileContext = DK.Code.FileContext.ServerTrigger;
                            fileContextSpan = wordSpan.Envelope(code.Span);
                        }
                        else if (code.ReadExactWholeWord("class"))
                        {
                            fileContext = DK.Code.FileContext.ServerClass;
                            fileContextSpan = wordSpan.Envelope(code.Span);
                        }
                        else if (code.ReadExactWholeWord("program"))
                        {
                            fileContext = DK.Code.FileContext.ServerProgram;
                            fileContextSpan = wordSpan.Envelope(code.Span);
                        }
                        else
                        {
                            fileContext = DK.Code.FileContext.ServerClass;
                            fileContextSpan = wordSpan;
                        }
                        continue;
                    case "const":
                        wordSpan = code.MovePeekedSpan();
                        if (const_) report.ReportItem(wordSpan, ErrorCode.DuplicateConstModifier);
                        const_ = true;
                        constSpan = wordSpan;
                        break;
                    case "static":
                        wordSpan = code.MovePeekedSpan();
                        if (static_) report.ReportItem(wordSpan, ErrorCode.DuplicateStaticModifier);
                        static_ = true;
                        staticSpan = wordSpan;
                        break;

                }
                break;
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

            if (Privacy.HasValue && Privacy != Files.Privacy.Private) report.ReportItem(PrivacySpan, ErrorCode.MemberVariableMustBePrivate);
        }

        public void CheckForConstant(ISourceCodeReporter report)
        {
            if (FileContext != null) report.ReportItem(FileContextSpan, ErrorCode.InvalidFileContext);
        }
    }
}
