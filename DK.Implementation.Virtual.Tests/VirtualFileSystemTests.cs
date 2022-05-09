using DK.AppEnvironment;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DK.Implementation.Virtual.Tests
{
    [TestFixture]
    class VirtualFileSystemTests
    {
        [TestCase(@"X:\tests", @"X:\tests")]
        [TestCase(@"x:\tests", @"X:\tests")]
        [TestCase(@"x:\TESTS", @"X:\tests")]
        [TestCase(@"x:\tests", @"X:\TESTS")]
        [TestCase(@"x:\tests", @"X:\tests\")]
        [TestCase(@"x:\tests\", @"X:\tests")]
        [TestCase(@"x:\tests", @"X:\tests\")]
        [TestCase(@"\tests", @"X:\tests")]
        [TestCase(@"tests", @"X:\tests")]
        [TestCase(@"X:\\tests", @"X:\tests")]
        [TestCase(@"X:\\\tests", @"X:\tests")]
        public void CreateDirectory(string createPath, string absolutePath)
        {
            var fs = new VirtualFileSystem();
            fs.CreateDirectory(createPath);
            Assert.IsTrue(fs.DirectoryExists(absolutePath));
        }

        [TestCase(@"X:\tests\subdir", @"X:\tests\subdir")]
        [TestCase(@"x:\tests\subdir", @"X:\tests\subdir")]
        [TestCase(@"x:\TESTS\SUBDIR", @"X:\tests\subdir")]
        [TestCase(@"x:\tests\subdir", @"X:\TESTS\SUBDIR")]
        [TestCase(@"\tests\subdir", @"X:\tests\subdir")]
        [TestCase(@"tests\subdir", @"X:\tests\subdir")]
        [TestCase(@"x:\tests\..\subdir", @"X:\subdir")]
        [TestCase(@"x:\tests\.\subdir", @"X:\tests\subdir")]
        [TestCase(@"x:\tests\..\.\subdir", @"X:\subdir")]
        [TestCase(@"x:\tests\..\.\tests\.\subdir", @"X:\tests\subdir")]
        [TestCase(@"X:\\tests\subdir", @"X:\tests\subdir")]
        [TestCase(@"X:\\tests\\subdir", @"X:\tests\subdir")]
        public void CreateDirectorySub(string createPath, string absolutePath)
        {
            var fs = new VirtualFileSystem();
            fs.CreateDirectory(@"X:\tests");
            fs.CreateDirectory(createPath);
            Assert.IsTrue(fs.DirectoryExists(absolutePath));
        }

        [TestCase(@"x:\tests", @"x:\tests")]
        [TestCase(@"x:\tests\test1", @"x:\tests\test1")]
        [TestCase(@"x:\tests\test1\logs", @"x:\tests\test1\logs")]
        public void CreateDirectoryRecursive(string createPath, string absolutePath)
        {
            var fs = new VirtualFileSystem();
            fs.CreateDirectoryRecursive(createPath);
            Assert.IsTrue(fs.DirectoryExists(absolutePath));
        }

        [Test]
        public void FileContent()
        {
            var fs = new VirtualFileSystem();
            fs.CreateDirectoryRecursive(@"x:\tests");
            fs.WriteFileText(@"x:\tests\file.txt", "Test Content");

            Assert.IsTrue(fs.FileExists(@"x:\tests\file.txt"));
            Assert.IsFalse(fs.FileExists(@"x:\tests\file.log"));
            Assert.IsFalse(fs.FileExists(@"x:\tests\blah.txt"));
            Assert.IsFalse(fs.FileExists(@"x:\tests"));

            Assert.AreEqual("Test Content", fs.GetFileText(@"x:\tests\file.txt"));
            Assert.AreEqual(Encoding.UTF8.GetBytes("Test Content"), fs.GetFileBytes(@"x:\tests\file.txt"));
        }

        [TestCase("*.txt", "aaa.txt|bbb.txt|ccc.txt|abc.txt")]
        [TestCase("a*.txt", "aaa.txt|abc.txt")]
        [TestCase("ab*.txt", "abc.txt")]
        [TestCase("a??.txt", "aaa.txt|abc.txt")]
        [TestCase("a?a.txt", "aaa.txt")]
        [TestCase("*.log", "xyz.log|xxx.log|yyy.log|zzz.log")]
        [TestCase("*.*", "aaa.txt|bbb.txt|ccc.txt|abc.txt|xyz.log|xxx.log|yyy.log|zzz.log")]
        [TestCase("*", "aaa.txt|bbb.txt|ccc.txt|abc.txt|xyz.log|xxx.log|yyy.log|zzz.log")]
        public void FilesInDirectoryWildcards(string searchPattern, string expectedFilesString)
        {
            var fs = new VirtualFileSystem();
            fs.CreateDirectoryRecursive(@"x:\test");
            fs.WriteFileText(@"x:\test\aaa.txt", "");
            fs.WriteFileText(@"x:\test\bbb.txt", "");
            fs.WriteFileText(@"x:\test\ccc.txt", "");
            fs.WriteFileText(@"x:\test\abc.txt", "");
            fs.WriteFileText(@"x:\test\xyz.log", "");
            fs.WriteFileText(@"x:\test\xxx.log", "");
            fs.WriteFileText(@"x:\test\yyy.log", "");
            fs.WriteFileText(@"x:\test\zzz.log", "");

            var expectedFiles = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            foreach (var file in expectedFilesString.Split('|')) expectedFiles[file] = true;

            var files = fs.GetFilesInDirectory(@"x:\test", searchPattern);

            foreach (var pathName in files)
            {
                Assert.IsTrue(PathUtil.GetDirectoryName(pathName).EqualsI(@"x:\test"));
                var fileName = PathUtil.GetFileName(pathName);

                Assert.True(expectedFiles.Remove(fileName), $"File '{pathName}' was not expected.");
            }

            Assert.AreEqual(0, expectedFiles.Count, $"Expected files were not returned: {string.Join("|", expectedFiles.Keys)}");
        }
    }
}
