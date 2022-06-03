using DK.Code;
using System;

namespace DKX.Compilation.Jobs
{
    struct FileTarget
    {
        private FileContext _fileContext;
        private string _relPathName;

        public FileTarget(FileContext fileContext, string relPathName)
        {
            _fileContext = fileContext;
            _relPathName = relPathName ?? throw new ArgumentNullException(nameof(relPathName));
        }

        public FileContext FileContext => _fileContext;
        public string RelativePathName => _relPathName;

        public override string ToString() => $"{_fileContext} {_relPathName}";

        public override bool Equals(object obj)
        {
            if (obj == null || obj.GetType() != typeof(FileTarget)) return false;
            var ft = (FileTarget)obj;
            return _fileContext == ft._fileContext && _relPathName == ft._relPathName;
        }

        public override int GetHashCode() => _fileContext.GetHashCode() * 13 + (_relPathName?.GetHashCode() ?? 0);

        public static bool operator ==(FileTarget a, FileTarget b) => a._fileContext == b._fileContext && a._relPathName == b._relPathName;
        public static bool operator !=(FileTarget a, FileTarget b) => a._fileContext != b._fileContext || a._relPathName != b._relPathName;
    }
}
