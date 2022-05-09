using DK.Code;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DKX.Compilation
{
    public struct ReportItem
    {
        private string _pathName;
        private CodeSpan _span;
        private ErrorCode _code;
        private object[] _args;

        public ReportItem(string pathName, CodeSpan span, ErrorCode code, params object[] args)
        {
            _pathName = pathName;
            _span = span;
            _code = code;
            _args = args;
        }

        public ErrorCode Code => _code;
        public string PathName => _pathName;
        public ErrorSeverity Severity => _code.GetSeverity();
        public CodeSpan Span => _span;

        public override string ToString()
        {
            var desc = _code.GetDescription();
            if (_args != null && _args.Length > 0) return string.Format(desc, _args);
            return desc;
        }
    }

    public enum ErrorSeverity
    {
        Error,
        Warning
    }
}
