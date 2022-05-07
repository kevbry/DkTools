using System;

namespace DK.AppEnvironment
{
    public static class PathUtil
    {
        public const char DirectorySeparatorChar = '\\';
        public const string DirectorySeparatorString = "\\";

        public static bool TrySplitPath(string path, out string root, out string directories, out string fileName)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                root = null;
                directories = null;
                fileName = null;
                return false;
            }

            // Root
            var pos = 0;
            if (path.Length >= 2 && char.IsLetter(path[0]) && path[1] == ':')   // X:\
            {
                if (path.Length >= 3 && path[2] == DirectorySeparatorChar)
                {
                    root = path.Substring(0, 3);
                    pos = 3;
                }
                else
                {
                    root = path.Substring(0, 2);
                    pos = 2;
                }
            }
            else if (path.Length >= 3 && path[0] == DirectorySeparatorChar && path[1] == DirectorySeparatorChar && path[2] != DirectorySeparatorChar) // \\server\\share
            {
                var nextSlash = path.IndexOf(DirectorySeparatorChar, 2);
                if (nextSlash < 0)
                {
                    // The entire path is just the root
                    root = path;
                    directories = string.Empty;
                    fileName = string.Empty;
                    return true;
                }

                var serverName = path.Substring(2, nextSlash - 2);

                // Look for the share name after the server name.
                while (nextSlash < path.Length && path[nextSlash] == DirectorySeparatorChar) nextSlash++;
                var slash2 = path.IndexOf(DirectorySeparatorChar, nextSlash);
                string shareName;
                if (slash2 < 0)
                {
                    shareName = path.Substring(nextSlash);
                    pos = path.Length;
                }
                else
                {
                    shareName = path.Substring(nextSlash, slash2 - nextSlash);
                    pos = slash2 + 1;
                }
                root = string.Concat(DirectorySeparatorChar, DirectorySeparatorChar, serverName, DirectorySeparatorChar, shareName);
                if (pos >= path.Length)
                {
                    directories = string.Empty;
                    fileName = string.Empty;
                    return true;
                }
            }
            else
            {
                root = string.Empty;
                while (pos < path.Length && path[pos] == DirectorySeparatorChar) pos++;
            }

            // Directories
            while (pos < path.Length && path[pos] == DirectorySeparatorChar) pos++;
            var lastSlash = path.LastIndexOf(DirectorySeparatorChar);
            if (lastSlash < pos)
            {
                directories = string.Empty;
                fileName = path.Substring(pos);
                return true;
            }

            directories = path.Substring(pos, lastSlash - pos).TrimEnd(DirectorySeparatorChar);
            pos = lastSlash + 1;

            // File name
            if (pos < path.Length)
            {
                fileName = path.Substring(pos, path.Length - pos);
            }
            else
            {
                fileName = string.Empty;
            }

            return true;
        }

        public static string GetFileName(string pathName)
        {
            if (TrySplitPath(pathName, out var _, out var _, out var fileName)) return fileName;
            return string.Empty;
        }

        public static string GetExtension(string pathName)
        {
            if (pathName == null) throw new ArgumentNullException(nameof(pathName));
            if (pathName.EndsWith(DirectorySeparatorString)) pathName = pathName.TrimEnd(DirectorySeparatorChar);

            // Find last '.' after the last '\'
            var s = pathName.LastIndexOf(DirectorySeparatorChar);
            var d = pathName.LastIndexOf('.');
            if (d >= 0 && d > s) return pathName.Substring(d);
            return string.Empty;
        }

        public static string GetFileNameWithoutExtension(string pathName)
        {
            var fileName = GetFileName(pathName);

            var d = fileName.LastIndexOf('.');
            if (d >= 0) return fileName.Substring(0, d);
            return fileName;
        }

        public static string GetDirectoryName(string pathName)
        {
            if (TrySplitPath(pathName, out var root, out var directories, out var fileName))
            {
                if (!string.IsNullOrEmpty(directories)) return string.Concat(root.TrimEnd(DirectorySeparatorChar), DirectorySeparatorChar, directories);
                if (!string.IsNullOrEmpty(fileName)) return root;
            }
            return string.Empty;
        }

        public static string CombinePath(string path1, string path2)
        {
            if (IsPathRooted(path2)) return path2;

            path2 = path2.TrimStart(DirectorySeparatorChar);

            if (!string.IsNullOrEmpty(path1))
            {
                if (!string.IsNullOrEmpty(path2))
                {
                    if (path1.EndsWith(DirectorySeparatorString))
                    {
                        if (path2.StartsWith(DirectorySeparatorString))
                        {
                            return string.Concat(path1, path2.Substring(1));
                        }
                        else
                        {
                            return string.Concat(path1, path2);
                        }
                    }
                    else
                    {
                        if (path2.StartsWith(DirectorySeparatorString))
                        {
                            return string.Concat(path1, path2);
                        }
                        else
                        {
                            return string.Concat(path1, DirectorySeparatorString, path2);
                        }
                    }
                }
                else
                {
                    return path1;
                }
            }
            else
            {
                return path2;
            }
        }

        public static string CombinePath(params string[] parts)
        {
            if (parts.Length == 0) return string.Empty;
            if (parts.Length == 1) return parts[0];

            var path = CombinePath(parts[0], parts[1]);
            for (int i = 2, ii = parts.Length; i < ii; i++) path = CombinePath(path, parts[i]);
            return path;
        }

        public static bool IsPathRooted(string path)
        {
            if (string.IsNullOrEmpty(path)) return false;
            if (path.StartsWith(DirectorySeparatorString)) return true;
            if (path.Length >= 2 && char.IsLetter(path[0]) && path[1] == ':') return true;
            return false;
        }
    }
}
