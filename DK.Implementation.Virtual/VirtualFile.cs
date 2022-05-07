using System;
using System.Linq;

namespace DK.Implementation.Virtual
{
    public class VirtualFile
    {
        private VirtualFileSystem _fs;
        private VirtualDirectory _parent;
        private string _name;
        private byte[] _content;
        private DateTime _modified;

        public VirtualFile(VirtualFileSystem fileSystem, VirtualDirectory parent, string name, byte[] content = null)
        {
            _fs = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            _parent = parent ?? throw new ArgumentNullException(nameof(parent));
            _name = name ?? throw new ArgumentNullException(nameof(name));
            _content = content;
            _modified = _fs.CurrentDate;

            if (string.IsNullOrWhiteSpace(_name)) throw new InvalidVirtualFileNameException(_name);

            var badChars = _fs.GetInvalidFileNameChars();
            foreach (var ch in _name)
            {
                if (badChars.Contains(ch)) throw new InvalidVirtualFileNameException(_name);
            }
        }

        public byte[] Content { get => _content ?? Constants.EmptyByteArray; set => _content = value ?? throw new ArgumentNullException(nameof(value)); }
        public string FullPathName => string.Concat(_parent.FullPath, VirtualFileSystem.DelimString, _name);
        public DateTime ModifiedDate { get => _modified; set => _modified = value; }
        public string Name => _name;
        public VirtualDirectory Parent => _parent;
    }
}
