using DK;
using DK.Code;
using System.Text;

namespace DKX.Compilation.ReportItems
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

        public ReportItem(string pathName, string source, CodeSpan span, ErrorCode code, params object[] args)
        {
            _pathName = pathName;
            _code = code;
            _args = args;

            if (span.Start > source.Length)
            {
                _startLine = -1;
                _startCh = -1;
                _endLine = -1;
                _endCh = -1;
            }
            else
            {
                StringHelper.CalcLineAndPosFromOffset(source, span.Start, out _startLine, out _startCh);
                if (span.End > source.Length)
                {
                    _endLine = _startLine;
                    _endCh = _startCh;
                }
                else
                {
                    StringHelper.CalcLineAndPosFromOffset(source, span.End, out _endLine, out _endCh);
                }
            }
        }

        public ReportItem(string pathName, string source, int pos, ErrorCode code, params object[] args)
        {
            _pathName = pathName;
            _code = code;
            _args = args;

            if (pos > source.Length)
            {
                _startLine = -1;
                _startCh = -1;
                _endLine = -1;
                _endCh = -1;
            }
            else
            {
                StringHelper.CalcLineAndPosFromOffset(source, pos, out _startLine, out _startCh);
                _endLine = _startLine;
                _endCh = _startCh;
            }
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

        public static ReportItem FromOneBased(string pathName, int startLine, int startPos, int endLine, int endPos, ErrorCode errorCode, params object[] args)
        {
            return new ReportItem(pathName, startLine - 1, startPos - 1, endLine - 1, endPos - 1, errorCode, args);
        }

        public static ReportItem FromOneBased(string pathName, int startLine, int startPos, ErrorCode errorCode, params object[] args)
        {
            return new ReportItem(pathName, startLine - 1, startPos - 1, -1, -1, errorCode, args);
        }
    }

    public enum ErrorSeverity
    {
        Error,
        Warning
    }
}
