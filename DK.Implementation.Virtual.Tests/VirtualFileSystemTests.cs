using DK.AppEnvironment;
using NUnit.Framework;
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
    }
}
