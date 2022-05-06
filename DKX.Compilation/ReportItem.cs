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
        private FilePosition _pos;
        private ErrorCode _code;
        private object[] _args;

        public ReportItem(FilePosition pos, ErrorCode code, params object[] args)
        {
            _pos = pos;
            _code = code;
            _args = args;
        }

        public FilePosition Location => _pos;
        public ErrorCode Code => _code;
        public ErrorSeverity Severity => _code.GetSeverity();

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
