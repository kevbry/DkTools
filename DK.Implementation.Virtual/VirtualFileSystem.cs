using DK.AppEnvironment;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DK.Implementation.Virtual
{
    public class VirtualFileSystem : IFileSystem
    {
        private VirtualDirectory _root;

        public const char DelimChar = '\\';
        public const string DelimString = "\\";
        public const string DriveStart = "X:";

        public VirtualFileSystem()
        {
            _root = new VirtualDirectory(this, parent: null, DriveStart);
        }

        private string CleanPath(string path)
        {
            var chain = new List<string>();

            if (path.Length >= 1 && path[0] == DelimChar) chain.Add(DriveStart);

            foreach (var part in path.Split(DelimChar))
            {
                if (part == "." || string.IsNullOrWhiteSpace(part))
                {
                    continue;
                }
                else if (part == "..")
                {
                    if (chain.Count == 0) throw new InvalidVirtualPathException(path);
                    chain.RemoveAt(chain.Count - 1);
                }
                else
                {
                    chain.Add(part);
                }
            }

            return string.Join(DelimString, chain.ToArray());
        }

        private VirtualDirectory FindDirectoryOrNull(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return null;

            var currentDir = _root;
            var pos = 0;
            var len = path.Length;
            var sb = new StringBuilder();

            if (path.StartsWith(DriveStart, StringComparison.OrdinalIgnoreCase)) pos = DriveStart.Length;
            else if (path.Length >= 1 && path[0] == DelimChar) pos = 1; // Starts with \
            else pos = 0;   // Assuming the working directory is X:\

            while (pos < len)
            {
                var ch = path[pos];

                // Ignore redundant \'s
                if (ch == DelimChar)
                {
                    pos++;
                    continue;
                }

                // Find the next part of the path
                sb.Clear();
                while (pos < len && (ch = path[pos]) != DelimChar)
                {
                    sb.Append(ch);
                    pos++;
                }

                var name = sb.ToString();
                if (string.IsNullOrWhiteSpace(name)) return null;
                if (name == ".") continue;
                if (name == "..")
                {
                    if (currentDir.Parent == null) return null;
                    currentDir = currentDir.Parent;
                    continue;
                }

                var subDir = currentDir.GetSubDirectoryOrNull(name);
                if (subDir == null) return null;
                currentDir = subDir;
            }

            return currentDir;
        }

        private VirtualFile FindFileOrNull(string pathName)
        {
            var dir = FindDirectoryOrNull(PathUtil.GetDirectoryName(pathName));
            if (dir == null) return null;

            return dir.GetFileOrNull(PathUtil.GetFileName(pathName));
        }

        public bool DirectoryExists(string path)
        {
            path = CleanPath(path);
            return FindDirectoryOrNull(path) != null;
        }

        public bool FileExists(string pathName)
        {
            pathName = CleanPath(pathName);
            return FindFileOrNull(pathName) != null;
        }

        public string CombinePath(string parentPath, string childPath) => PathUtil.CombinePath(parentPath, childPath);

        public string CombinePath(params string[] pathComponents) => PathUtil.CombinePath(pathComponents);

        public string GetFullPath(string path) => CleanPath(path);

        public string GetParentDirectoryName(string path) => PathUtil.GetDirectoryName(path);

        public string GetFileName(string pathName) => PathUtil.GetFileName(pathName);

        public string GetExtension(string pathName) => PathUtil.GetExtension(pathName);

        public string GetFileNameWithoutExtension(string pathName) => PathUtil.GetFileNameWithoutExtension(pathName);

        public IEnumerable<string> GetFilesInDirectory(string path)
        {
            path = CleanPath(path);
            var dir = FindDirectoryOrNull(path);
            if (dir != null) return dir.Files.Select(x => x.FullPathName);
            return Constants.EmptyStringArray;
        }

        public IEnumerable<string> GetFilesInDirectory(string path, string pattern)
        {
            path = CleanPath(path);
            var dir = FindDirectoryOrNull(path);
            if (dir == null) return Constants.EmptyStringArray;

            // Make a regular expression that simulates the traditional wildcard pattern.
            var rx = new StringBuilder();
            foreach (var ch in pattern)
            {
                if (ch.IsWordChar(false)) rx.Append(ch);
                else if (char.IsWhiteSpace(ch)) rx.Append("\\s");
                else if (ch == '*') rx.Append(".*");
                else if (ch == '?') rx.Append(".");
                else rx.AppendFormat("\\{0}", ch);
            }
            rx.Append('$');
            var regex = new Regex(rx.ToString(), RegexOptions.IgnoreCase);

            return dir.Files
                .Where(x => regex.IsMatch(x.Name))
                .Select(x => x.FullPathName);
        }

        public IEnumerable<string> GetDirectoriesInDirectory(string path)
        {
            path = CleanPath(path);
            var dir = FindDirectoryOrNull(path);
            if (dir != null) return dir.Directories.Select(x => x.FullPath);
            return Constants.EmptyStringArray;
        }

        public char[] GetInvalidPathChars() => _invalidPathChars ?? (_invalidPathChars = System.IO.Path.GetInvalidPathChars());
        private static char[] _invalidPathChars;

        public char[] GetInvalidFileNameChars() => _invalidFileNameChars ?? (_invalidFileNameChars = System.IO.Path.GetInvalidFileNameChars());
        private static char[] _invalidFileNameChars;

        public bool IsDirectoryHiddenOrSystem(string path) => false;

        public string ReadFileText(string pathName)
        {
            pathName = CleanPath(pathName);
            var file = FindFileOrNull(pathName);
            if (file == null) throw new VirtualFileNotFoundException(pathName);

            var bytes = file.Content;
            if (bytes == null) return string.Empty;
            return Encoding.UTF8.GetString(bytes);
        }

        public Task<string> ReadFileTextAsync(string pathName) => Task.FromResult(ReadFileText(pathName));

        public byte[] ReadFileBytes(string pathName)
        {
            pathName = CleanPath(pathName);
            var file = FindFileOrNull(pathName);
            if (file == null) throw new VirtualFileNotFoundException(pathName);
            return file.Content;
        }

        public Task<byte[]> ReadFileBytesAsync(string pathName) => Task.FromResult(ReadFileBytes(pathName));

        public void WriteFileText(string pathName, string text)
        {
            pathName = CleanPath(pathName);
            var parentPath = PathUtil.GetDirectoryName(pathName);
            var name = PathUtil.GetFileName(pathName);

            if (string.IsNullOrEmpty(parentPath))
            {
                _root.WriteFile(name, Encoding.UTF8.GetBytes(text));
                return;
            }

            var dir = FindDirectoryOrNull(parentPath);
            if (dir == null) throw new VirtualDirectoryNotFoundException(parentPath);

            dir.WriteFile(name, Encoding.UTF8.GetBytes(text));
        }

        public Task WriteFileTextAsync(string pathName, string text)
        {
            WriteFileText(pathName, text);
            return Task.CompletedTask;
        }

        public void WriteFileBytes(string pathName, byte[] data)
        {
            pathName = CleanPath(pathName);
            var parentPath = PathUtil.GetDirectoryName(pathName);
            var name = PathUtil.GetFileName(pathName);

            if (string.IsNullOrEmpty(parentPath))
            {
                _root.WriteFile(name, data);
                return;
            }

            var dir = FindDirectoryOrNull(parentPath);
            if (dir == null) throw new VirtualDirectoryNotFoundException(parentPath);

            dir.WriteFile(name, data);
        }

        public Task WriteFileBytesAsync(string pathName, byte[] data)
        {
            WriteFileBytes(pathName, data);
            return Task.CompletedTask;
        }

        public void TouchFile(string pathName)
        {
            pathName = CleanPath(pathName);
            var file = FindFileOrNull(pathName);
            if (file == null) throw new VirtualFileNotFoundException(pathName);

            file.ModifiedDate = CurrentDate;
        }

        public void CreateDirectory(string path)
        {
            path = CleanPath(path);
            var parentPath = PathUtil.GetDirectoryName(path);
            var dirName = PathUtil.GetFileName(path);

            if (string.IsNullOrEmpty(parentPath))
            {
                _root.CreateDirectory(dirName);
                return;
            }

            var dir = FindDirectoryOrNull(parentPath);
            if (dir == null) throw new VirtualDirectoryNotFoundException(parentPath);

            dir.CreateDirectory(PathUtil.GetFileName(path));
        }

        public DateTime GetFileModifiedDate(string pathName)
        {
            pathName = CleanPath(pathName);
            var file = FindFileOrNull(pathName);
            if (file == null) throw new VirtualFileNotFoundException(pathName);

            return file.ModifiedDate;
        }

        public DateTime CurrentDate => DateTime.Now + DateOffset;

        public TimeSpan DateOffset { get; set; }

        public void DeleteFile(string pathName)
        {
            pathName = CleanPath(pathName);

            var parentPath = PathUtil.GetDirectoryName(pathName);
            var dir = FindDirectoryOrNull(parentPath);
            if (dir == null) throw new VirtualDirectoryNotFoundException(parentPath);

            dir.DeleteFile(PathUtil.GetFileName(pathName));
        }

        public void DeleteDirectory(string path)
        {
            path = CleanPath(path);

            var parentPath = PathUtil.GetDirectoryName(path);
            var dir = FindDirectoryOrNull(parentPath);
            if (dir == null) throw new VirtualDirectoryNotFoundException(parentPath);

            dir.DeleteDirectory(PathUtil.GetFileName(path));
        }
    }
}
