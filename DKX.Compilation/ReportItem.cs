using DK.Code;
using System.Text;

namespace DKX.Compilation
{
    public struct ReportItem
    {
        private string _pathName;
        private int _startLine;
        private int _startCh;
        private int _endLine;
        private int _endCh;
        private ErrorCode _code;
        private object[] _args;

        public static readonly ReportItem[] EmptyArray = new ReportItem[0];

        public ReportItem(string pathName, int startLine, int startCh, int endLine, int endCh, ErrorCode code, params object[] args)
        {
            _pathName = pathName;
            _startLine = startLine;
            _startCh = startCh;
            _endLine = endLine;
            _endCh = endCh;
            _code = code;
            _args = args;
        }

        public ErrorCode Code => _code;
        public string PathName => _pathName;
        public ErrorSeverity Severity => _code.GetSeverity();

        public override string ToString()
        {
            var sb = new StringBuilder();
            if (!string.IsNullOrEmpty(_pathName))
            {
                sb.Append(_pathName);
                if (_startLine >= 0)
                {
                    sb.Append('(');
                    sb.Append(_startLine + 1);
                    sb.Append(',');
                    sb.Append(_startCh + 1);
                    if (_endLine >= 0 && (_startLine != _endLine || _startCh != _endCh))
                    {
                        sb.Append(',');
                        sb.Append(_endLine + 1);
                        sb.Append(',');
                        sb.Append(_endCh + 1);
                    }
                    sb.Append(')');
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

    public interface IReporter
    {
        void ReportItem(int pos, ErrorCode code, params object[] args);

        void ReportItem(CodeSpan span, ErrorCode code, params object[] args);
    }
}
