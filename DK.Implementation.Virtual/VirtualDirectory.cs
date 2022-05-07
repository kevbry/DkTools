using System;
using System.Collections.Generic;
using System.Linq;

namespace DK.Implementation.Virtual
{
    public class VirtualDirectory
    {
        private VirtualFileSystem _fs;
        private string _name;
        private VirtualDirectory _parent;
        private Dictionary<string, VirtualDirectory> _subDirs = new Dictionary<string, VirtualDirectory>(StringComparer.OrdinalIgnoreCase);
        private Dictionary<string, VirtualFile> _files = new Dictionary<string, VirtualFile>(StringComparer.OrdinalIgnoreCase);

        public VirtualDirectory(VirtualFileSystem fileSystem, VirtualDirectory parent, string name)
        {
            _fs = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            _parent = parent;
            _name = name ?? throw new ArgumentNullException(nameof(name));

            if (string.IsNullOrWhiteSpace(_name)) throw new InvalidVirtualDirectoryNameException(_name);

            if (_parent != null)
            {
                var badChars = _fs.GetInvalidFileNameChars();
                foreach (var ch in _name)
                {
                    if (badChars.Contains(ch)) throw new InvalidVirtualDirectoryNameException(_name);
                }
            }
        }

        public IEnumerable<VirtualDirectory> Directories => _subDirs.Values;
        public IEnumerable<VirtualFile> Files => _files.Values;
        public string FullPath =>  _parent != null ? string.Concat(_parent.FullPath, VirtualFileSystem.DelimString, _name) : string.Concat(_name, VirtualFileSystem.DelimString);
        public string Name => _name;
        public VirtualDirectory Parent => _parent;

        public VirtualDirectory GetSubDirectoryOrNull(string name)
        {
            if (_subDirs.TryGetValue(name, out var subDir)) return subDir;
            return null;
        }

        public VirtualFile GetFileOrNull(string name)
        {
            if (_files.TryGetValue(name, out var file)) return file;
            return null;
        }

        public void WriteFile(string name, byte[] bytes)
        {
            _files[name] = new VirtualFile(_fs, this, name, bytes);
        }

        public void CreateDirectory(string name)
        {
            if (!_subDirs.ContainsKey(name)) _subDirs[name] = new VirtualDirectory(_fs, this, name);
        }
    }
}
