using System;
using System.Collections.Generic;

namespace DK.AppEnvironment
{
    public interface IFileSystem
    {
        bool DirectoryExists(string path);

        bool FileExists(string pathName);

        string CombinePath(string parentPath, string childPath);

        string CombinePath(params string[] pathComponents);

        string GetFullPath(string path);

        string GetParentDirectoryName(string path);

        string GetFileName(string pathName);

        string GetExtension(string pathName);

        string GetFileNameWithoutExtension(string pathName);

        IEnumerable<string> GetFilesInDirectory(string path);

        IEnumerable<string> GetFilesInDirectory(string path, string pattern);

        IEnumerable<string> GetDirectoriesInDirectory(string path);

        char[] GetInvalidPathChars();

        char[] GetInvalidFileNameChars();

        bool IsDirectoryHiddenOrSystem(string path);

        string GetFileText(string pathName);

        byte[] GetFileBytes(string pathName);

        void WriteFileText(string pathName, string text);

        void WriteFileBytes(string pathName, byte[] data);

        void CreateDirectory(string path);

        DateTime GetFileModifiedDate(string pathName);

        void DeleteFile(string pathName);

        void DeleteDirectory(string path);
    }

    public static class IFileSystemUtil
    {
        public static void CreateDirectoryRecursive(this IFileSystem fs, string path)
        {
            if (string.IsNullOrEmpty(path)) throw new System.IO.DirectoryNotFoundException(path);

            if (fs.DirectoryExists(path)) return;

            var parentPath = PathUtil.GetDirectoryName(path);

            if (!string.IsNullOrEmpty(parentPath) && !fs.DirectoryExists(parentPath))
            {
                fs.CreateDirectoryRecursive(parentPath);
            }

            fs.CreateDirectory(path);
        }

        public static IEnumerable<string> GetFilesInDirectoryRecursive(this IFileSystem fs, string dirPath)
        {
            foreach (var pathName in fs.GetFilesInDirectory(dirPath))
            {
                yield return pathName;
            }

            foreach (var path in fs.GetDirectoriesInDirectory(dirPath))
            {
                foreach (var pathName in fs.GetFilesInDirectoryRecursive(path))
                {
                    yield return pathName;
                }
            }
        }

        public static IEnumerable<string> GetFilesInDirectoryRecursive(this IFileSystem fs, string dirPath, string pattern)
        {
            foreach (var pathName in fs.GetFilesInDirectory(dirPath, pattern))
            {
                yield return pathName;
            }

            foreach (var path in fs.GetDirectoriesInDirectory(dirPath))
            {
                foreach (var pathName in fs.GetFilesInDirectoryRecursive(path, pattern))
                {
                    yield return pathName;
                }
            }
        }
    }
}
