using System;
using System.IO;
using NUnit.Framework;

namespace ST.Core.Tests
{
    /// <summary>文件访问层的读取与路径解析测试。</summary>
    public sealed class FileMgrReadTest
    {
        /// <summary>测试文件在外存资源目录下的相对路径。</summary>
        const string k_FileRelativePath = "filemgr_read_test.bin";

        /// <summary>每个用例结束后恢复全局状态并清理临时文件。</summary>
        [TearDown]
        public void TearDown()
        {
            FileMgr.SetResolvePolicy(null);
            FileMgr.isAccessLogEnabled = false;
            FilePath.ResetCache();

            string testPath = FilePath.persistentDataPath4Res + k_FileRelativePath;
            bool isExist = File.Exists(testPath);
            if (isExist)
            {
                File.Delete(testPath);
            }
        }

        /// <summary>缓冲区大于文件时返回文件真实长度。</summary>
        [Test]
        public void ReadSyncWithLargerBufferReturnsFileLength()
        {
            byte[] content = { 1, 2, 3, 4, 5 };
            string testPath = FilePath.persistentDataPath4Res + k_FileRelativePath;
            File.WriteAllBytes(testPath, content);

            byte[] buffer = new byte[content.Length + 16];
            int readLength = FileMgr.ReadSync(k_FileRelativePath, buffer);

            Assert.AreEqual(content.Length, readLength);
            Assert.AreEqual(content[4], buffer[4]);
        }

        /// <summary>权威清单命中时返回外存路径，不探测磁盘。</summary>
        [Test]
        public void AuthoritativePolicyUsesManifestWithoutDiskProbe()
        {
            FakeFileResolvePolicy policy = new FakeFileResolvePolicy();
            policy.isAuthoritative = true;
            policy.externalRelativePath = "manifest_only.bin";
            FileMgr.SetResolvePolicy(policy);

            string expectedPath = FilePath.persistentDataPath4Res + "manifest_only.bin";
            string resolvedPath = FileMgr.FindFilePathInExteral("manifest_only.bin");
            Assert.AreEqual(expectedPath, resolvedPath);

            string absentPath = FileMgr.FindFilePathInExteral("absent.bin");
            Assert.AreEqual(string.Empty, absentPath);
        }

        /// <summary>非权威清单回退到目录扫描。</summary>
        [Test]
        public void NonAuthoritativePolicyFallsBackToDirectoryScan()
        {
            FakeFileResolvePolicy policy = new FakeFileResolvePolicy();
            policy.isAuthoritative = false;
            policy.externalRelativePath = k_FileRelativePath;
            FileMgr.SetResolvePolicy(policy);

            byte[] content = { 9, 8, 7 };
            string testPath = FilePath.persistentDataPath4Res + k_FileRelativePath;
            File.WriteAllBytes(testPath, content);

            string resolvedPath = FileMgr.FindFilePathInExteral(k_FileRelativePath);
            string expectedPath = FilePath.persistentDataPath4Res + k_FileRelativePath;
            Assert.AreEqual(expectedPath, resolvedPath);
        }

        /// <summary>Windows 下超长路径会补扩展前缀。</summary>
        [Test]
        public void WindowsWarpPathAddsExtendedPrefix()
        {
            string longPath = "C:/" + new string('a', 300) + "/file.bin";
            string warpPath = FilePath.GetWindowsWarpPath(longPath);

#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            bool isExtendedPath = warpPath.StartsWith(@"\\?\", StringComparison.Ordinal);
            Assert.IsTrue(isExtendedPath);
#else
            Assert.AreEqual(longPath, warpPath);
#endif
        }
    }
}

