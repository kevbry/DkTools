using DK.AppEnvironment;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace DK.Implementation.Windows
{
    public class WindowsFileSystem : IFileSystem
    {
        public bool DirectoryExists(string path) => Directory.Exists(path);

        public bool FileExists(string pathName) => File.Exists(pathName);

        public string CombinePath(string parentPath, string childPath) => Path.Combine(parentPath, childPath);

        public string CombinePath(params string[] pathComponents) => Path.Combine(pathComponents);

        public string GetFullPath(string path) => Path.GetFullPath(path);

        public string GetParentDirectoryName(string path) => Path.GetDirectoryName(path);

        public string GetFileName(string pathName) => Path.GetFileName(pathName);

        public string GetExtension(string pathName) => Path.GetExtension(pathName);

        public string GetFileNameWithoutExtension(string pathName) => Path.GetFileNameWithoutExtension(pathName);

        public IEnumerable<string> GetFilesInDirectory(string path) => Directory.GetFiles(path);

        public IEnumerable<string> GetFilesInDirectory(string path, string pattern) => Directory.GetFiles(path, pattern);

        public IEnumerable<string> GetDirectoriesInDirectory(string path) => Directory.GetDirectories(path);

        public char[] GetInvalidPathChars() => Path.GetInvalidPathChars();

        public char[] GetInvalidFileNameChars() => Path.GetInvalidFileNameChars();

        public bool IsDirectoryHiddenOrSystem(string path) => (new DirectoryInfo(path).Attributes & (FileAttributes.System | FileAttributes.Hidden)) != 0;

        public string ReadFileText(string pathName) => File.ReadAllText(pathName);

        public async Task<string> ReadFileTextAsync(string pathName)
        {
            using (var fileStream = new FileStream(pathName, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, useAsync: true))
            using (var streamReader = new StreamReader(fileStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true))
            {
                return await streamReader.ReadToEndAsync();
            }
        }

        public byte[] ReadFileBytes(string pathName) => File.ReadAllBytes(pathName);

        public async Task<byte[]> ReadFileBytesAsync(string pathName)
        {
            using (var fileStream = new FileStream(pathName, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, useAsync: true))
            {
                var bytes = new byte[fileStream.Length];
                var numRead = await fileStream.ReadAsync(bytes, 0, bytes.Length);
                if (numRead != bytes.Length)
                {
                    var bytes2 = new byte[numRead];
                    Array.Copy(bytes, bytes2, numRead);
                    return bytes2;
                }
                return bytes;
            }
        }

        public void WriteFileText(string pathName, string text) => File.WriteAllText(pathName, text);

        public async Task WriteFileTextAsync(string pathName, string text)
        {
            var bytes = Encoding.UTF8.GetBytes(text);
            using (var fileStream = new FileStream(pathName, FileMode.Open, FileAccess.Write, FileShare.Read, bufferSize: 4096, useAsync: true))
            {
                await fileStream.WriteAsync(bytes, 0, bytes.Length);
            }
        }

        public void WriteFileBytes(string pathName, byte[] data) => File.WriteAllBytes(pathName, data);

        public async Task WriteFileBytesAsync(string pathName, byte[] data)
        {
            using (var fileStream = new FileStream(pathName, FileMode.Open, FileAccess.Write, FileShare.Read, bufferSize: 4096, useAsync: true))
            {
                await fileStream.WriteAsync(data, 0, data.Length);
            }
        }

        public void CreateDirectory(string path) => Directory.CreateDirectory(path);

        public DateTime GetFileModifiedDate(string pathName) => File.GetLastWriteTime(pathName);

        public void DeleteFile(string pathName)
        {
            var attribs = File.GetAttributes(pathName);
            if (attribs.HasFlag(FileAttributes.ReadOnly)) File.SetAttributes(pathName, attribs & ~FileAttributes.ReadOnly);

            File.Delete(pathName);
        }

        public void DeleteDirectory(string path)
        {
            foreach (var file in Directory.GetFiles(path)) DeleteFile(file);
            foreach (var dir in Directory.GetDirectories(path)) DeleteDirectory(dir);
            Directory.Delete(path);
        }
    }
}
