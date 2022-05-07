using DK.AppEnvironment;
using NUnit.Framework;

namespace DK.Common.Tests
{
    [TestFixture]
    public class PathUtilTests
    {
        [TestCase(@"c:\temp\log.txt", @"log.txt")]
        [TestCase(@"c:\log.txt", @"log.txt")]
        [TestCase(@"file.txt", @"file.txt")]
        [TestCase(@"temp\file.txt", @"file.txt")]
        [TestCase(@"temp\\file.txt", @"file.txt")]
        [TestCase(@"\temp\file.txt", @"file.txt")]
        [TestCase(@"\temp\\file.txt", @"file.txt")]
        [TestCase(@"X:\ccssrc1\prod\dict", @"dict")]
        [TestCase(@"X:\ccssrc1\prod\dict+", @"dict+")]
        [TestCase(@"X:\ccssrc1\prod\", @"")]
        [TestCase(@"X:\ccssrc1\", @"")]
        [TestCase(@"X:\", @"")]
        [TestCase(@"X:", @"")]
        [TestCase(@"\\servername\share\info.log", @"info.log")]
        [TestCase(@"\\servername\share\logs\info.log", @"info.log")]
        [TestCase(@"\\servername\share", @"")]
        [TestCase(@"\\servername\share\", @"")]
        [TestCase(@"\\servername\", @"")]
        [TestCase(@"\\servername", @"")]
        public void GetFileName(string input, string output)
        {
            //Assert.AreEqual(output, Path.GetFileName(input), "Test case does not match System.IO.Path");
            Assert.AreEqual(output, PathUtil.GetFileName(input));
        }

        [TestCase(@"c:\temp\log.txt", ".txt")]
        [TestCase(@"c:\temp\log.json.txt", ".txt")]
        [TestCase(@"\\servername\share\info.log", ".log")]
        [TestCase(@"file.txt", ".txt")]
        [TestCase(@"file.txt+", ".txt+")]
        [TestCase(@"X:\ccssrc1\prod\dict", "")]
        [TestCase(@"X:\ccssrc1\prod\dict+", "")]
        [TestCase(@"X:\ccssrc1\prod\", @"")]
        [TestCase(@"X:\ccssrc1\", @"")]
        [TestCase(@"X:\ccssrc1", @"")]
        [TestCase(@"X:\", @"")]
        [TestCase(@"X:", @"")]
        [TestCase(@".gitignore", @".gitignore")]
        [TestCase(@"X:\project\.gitignore", @".gitignore")]
        public void GetExtension(string input, string output)
        {
            //Assert.AreEqual(output, Path.GetExtension(input), "Test case does not match System.IO.Path");
            Assert.AreEqual(output, PathUtil.GetExtension(input));
        }

        [TestCase(@"c:\temp\log.txt", "log")]
        [TestCase(@"c:\temp\log.json.txt", "log.json")]
        [TestCase(@"\\servername\share\info.log", "info")]
        [TestCase(@"\\servername\share\logs\info.log", "info")]
        [TestCase(@"\\servername\share\logs", "logs")]
        [TestCase(@"\\servername\share\logs\", "")]
        [TestCase(@"\\servername\share", "")]
        [TestCase(@"\\servername\", "")]
        [TestCase(@"\\servername", "")]
        [TestCase(@"file.txt", "file")]
        [TestCase(@"file.txt+", "file")]
        [TestCase(@"X:\ccssrc1\prod\dict", "dict")]
        [TestCase(@"X:\ccssrc1\prod\dict+", "dict+")]
        [TestCase(@"X:\ccssrc1\prod\", @"")]
        [TestCase(@"X:\ccssrc1\", @"")]
        [TestCase(@"X:\ccssrc1", @"ccssrc1")]
        [TestCase(@"X:\", @"")]
        [TestCase(@"X:", @"")]
        [TestCase(@".gitignore", @"")]
        [TestCase(@"X:\project\.gitignore", @"")]
        public void GetFileNameWithoutExtension(string input, string output)
        {
            //Assert.AreEqual(output, Path.GetFileNameWithoutExtension(input), "Test case does not match System.IO.Path");
            Assert.AreEqual(output, PathUtil.GetFileNameWithoutExtension(input));
        }

        [TestCase(@"c:\temp\log.txt", @"c:\temp")]
        [TestCase(@"c:\temp\\log.txt", @"c:\temp")]
        [TestCase(@"c:\temp\dir\log.txt", @"c:\temp\dir")]
        [TestCase(@"c:\temp\dir", @"c:\temp")]
        [TestCase(@"c:\temp\dir\", @"c:\temp\dir")]
        [TestCase(@"c:\temp", @"c:\")]
        [TestCase(@"c:\temp\", @"c:\temp")]
        [TestCase(@"c:\", @"")]
        [TestCase(@"c:", @"")]
        [TestCase(@"\\server\share", @"")]
        [TestCase(@"\\server\", @"")]
        [TestCase(@"\\server", @"")]
        [TestCase(@"\\server\share\logfiles", @"\\server\share")]
        [TestCase(@"\\server\share\logfiles\", @"\\server\share\logfiles")]
        [TestCase(@"\\server\share\logfiles\log.txt", @"\\server\share\logfiles")]
        public void GetDirectoryName(string input, string output)
        {
            //Assert.AreEqual(output, Path.GetDirectoryName(input), "Test case does not match System.IO.Path");
            Assert.AreEqual(output, PathUtil.GetDirectoryName(input));
        }

        [TestCase(@"X:\ccssrc1\prod", @"cust.ct", @"X:\ccssrc1\prod\cust.ct")]
        [TestCase(@"X:\ccssrc1\prod\", @"cust.ct", @"X:\ccssrc1\prod\cust.ct")]
        [TestCase(@"X:\ccssrc1\prod\", @"\cust.ct", @"\cust.ct")]
        [TestCase(@"X:\ccssrc1\prod\\", @"\cust.ct", @"\cust.ct")]
        [TestCase(@"X:\ccssrc1\prod\\", @"\cust.ct\", @"\cust.ct\")]
        [TestCase(@"X:\ccssrc1\prod\\", @"", @"X:\ccssrc1\prod\\")]
        [TestCase(@"", @"cust.ct", @"cust.ct")]
        [TestCase(@"temp", @"log.txt", @"temp\log.txt")]
        [TestCase(@"temp", @"\log.txt", @"\log.txt")]
        [TestCase(@"X:\temp\file1.txt", @"X:\temp\file2.txt", @"X:\temp\file2.txt")]
        public void CombinePath(string path1, string path2, string output)
        {
            //Assert.AreEqual(output, Path.Combine(path1, path2), "Test case does not match System.IO.Path");
            Assert.AreEqual(output, PathUtil.CombinePath(path1, path2));
        }

        [TestCase(@"x:\ccssrc1", @"prod", @"cust.ct", @"x:\ccssrc1\prod\cust.ct")]
        [TestCase(@"x:\ccssrc1\", @"prod", @"cust.ct", @"x:\ccssrc1\prod\cust.ct")]
        [TestCase(@"x:\ccssrc1\", @"prod\", @"cust.ct", @"x:\ccssrc1\prod\cust.ct")]
        [TestCase(@"x:\ccssrc1", @"\prod", @"cust.ct", @"\prod\cust.ct")]
        [TestCase(@"x:\ccssrc1\", @"\prod\", @"cust.ct", @"\prod\cust.ct")]
        [TestCase(@"x:\ccssrc1", @"prod", @"\cust.ct", @"\cust.ct")]
        [TestCase(@"x:\ccssrc1", @"x:\prod", @"cust.ct", @"x:\prod\cust.ct")]
        [TestCase(@"x:\ccssrc1", @"x:\prod", @"x:\cust.ct", @"x:\cust.ct")]
        [TestCase(@"ccssrc1", @"prod", @"cust.ct", @"ccssrc1\prod\cust.ct")]
        [TestCase(@"", @"prod", @"cust.ct", @"prod\cust.ct")]
        [TestCase(@"ccssrc1", @"", @"cust.ct", @"ccssrc1\cust.ct")]
        [TestCase(@"ccssrc1", @"prod", @"", @"ccssrc1\prod")]
        public void CompinePath3(string path1, string path2, string path3, string output)
        {
            //Assert.AreEqual(output, Path.Combine(path1, path2, path3), "Test case does not match System.IO.Path");
            Assert.AreEqual(output, PathUtil.CombinePath(path1, path2, path3));
        }

        [TestCase(@"X:\ccssrc1\prod", true)]
        [TestCase(@"\ccssrc1\prod", true)]
        [TestCase(@"ccssrc1\prod", false)]
        [TestCase(@"log.txt", false)]
        [TestCase(@"\log.txt", true)]
        [TestCase(@"\", true)]
        [TestCase(@"", false)]
        [TestCase(@"\\servername\share\info.dat", true)]
        [TestCase(@"\\servername", true)]
        [TestCase(@"X:\", true)]
        [TestCase(@"X:", true)]
        [TestCase(@"X", false)]
        public void IsPathRooted(string path, bool result)
        {
            //Assert.AreEqual(result, Path.IsPathRooted(path), "Test case does not match System.IO.Path");
            Assert.AreEqual(result, PathUtil.IsPathRooted(path));
        }

        [TestCase(@"x:\ccssrc1\prod\cust.ct", @"x:\", @"ccssrc1\prod", @"cust.ct", true)]
        [TestCase(@"x:\ccssrc1\prod\\cust.ct", @"x:\", @"ccssrc1\prod", @"cust.ct", true)]
        [TestCase(@"x:\ccssrc1\prod", @"x:\", @"ccssrc1", @"prod", true)]
        [TestCase(@"x:\ccssrc1\prod\", @"x:\", @"ccssrc1\prod", @"", true)]
        [TestCase(@"\\server", @"\\server", @"", @"", true)]
        [TestCase(@"\\server\", @"\\server\", @"", @"", true)]
        [TestCase(@"\\server\\", @"\\server\", @"", @"", true)]
        [TestCase(@"\\server\share", @"\\server\share", @"", @"", true)]
        [TestCase(@"\\server\share\", @"\\server\share", @"", @"", true)]
        [TestCase(@"\\server\\share\", @"\\server\share", @"", @"", true)]
        [TestCase(@"\\server\share\file.txt", @"\\server\share", @"", @"file.txt", true)]
        [TestCase(@"\\server\share\logs\file.txt", @"\\server\share", @"logs", @"file.txt", true)]
        [TestCase(@"\\server\share\\logs\file.txt", @"\\server\share", @"logs", @"file.txt", true)]
        [TestCase(@"\\server\share\logs\\file.txt", @"\\server\share", @"logs", @"file.txt", true)]
        [TestCase(@"\\server\share\logs\gateway\file.txt", @"\\server\share", @"logs\gateway", @"file.txt", true)]
        public void TrySplitPath(string path, string expectedRoot, string expectedDirectories, string expectedFileName, bool expectSuccess)
        {
            Assert.AreEqual(expectSuccess, PathUtil.TrySplitPath(path, out var rootOut, out var directoriesOut, out var fileNameOut));
            if (!expectSuccess)
            {
                Assert.IsNull(rootOut);
                Assert.IsNull(directoriesOut);
                Assert.IsNull(fileNameOut);
                return;
            }

            //Assert.AreEqual(expectedRoot, Path.GetPathRoot(path), "Test case does not match System.IO.Path.GetPathRoot()");
            //Assert.AreEqual(expectedFileName, Path.GetFileName(path), "Test case does not match System.IO.Path.GetFileName()");

            Assert.AreEqual(expectedRoot, rootOut);
            Assert.AreEqual(expectedDirectories, directoriesOut);
            Assert.AreEqual(expectedFileName, fileNameOut);
        }
    }
}
