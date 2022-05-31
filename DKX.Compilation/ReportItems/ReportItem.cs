using DK;
using System.Text;
using System.Threading.Tasks;

namespace DKX.Compilation.ReportItems
{
    public struct ReportItem
    {
        private string _pathName;
        private Span _span;
        private ErrorCode _code;
        private object[] _args;

        public static readonly ReportItem[] EmptyArray = new ReportItem[0];

        public ReportItem(Span span, ErrorCode code, params object[] args)
        {
            _pathName = span.PathName;
            _span = span;
            _code = code;
            _args = args;
        }

        public ErrorCode Code => _code;
        public string PathName => _pathName;
        public ErrorSeverity Severity => _code.GetSeverity();

        public async Task<string> ToDisplayStringAsync(SourceCodeCache sourceCodeCache)
        {
            var sb = new StringBuilder();
            if (!string.IsNullOrEmpty(_pathName))
            {
                sb.Append(_pathName);

                if (_span.Start != 0 || _span.End != 0)
                {
                    var source = await sourceCodeCache.GetSourceCodeAsync(_pathName);
                    if (source != null)
                    {
                        int startLine = 0;
                        int startPos = 0;
                        if (_span.Start >= 0 && _span.Start <= source.Length) StringHelper.CalcLineAndPosFromOffset(source, _span.Start, out startLine, out startPos);

                        sb.Append('(');
                        sb.Append(startLine + 1);
                        sb.Append(',');
                        sb.Append(startPos + 1);

                        if (_span.End != _span.Start && _span.End >= 0 && _span.End <= source.Length)
                        {
                            StringHelper.CalcLineAndPosFromOffset(source, _span.End, out var endLine, out var endPos);

                            sb.Append(',');
                            sb.Append(endLine + 1);
                            sb.Append(',');
                            sb.Append(endPos + 1);
                        }

                        sb.Append(')');
                    }
                }
                sb.Append(": ");
            }

            sb.Append(Severity.ToString().ToLower());
            sb.Append(" DKX");
            sb.Append((int)_code);
            sb.Append(": ");

            var desc = _code.GetDescription();
            if (_args != null && _args.Length > 0) sb.Append(string.Format(desc, _args));
            else sb.Append(desc);

            return sb.ToString();
        }
    }

    public enum ErrorSeverity
    {
        Error,
        Warning
    }
}
