namespace SevenZip.Tests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading;

    using SevenZip;

    using NUnit.Framework;

    [TestFixture]
    public class SevenZipCompressorTests : TestBase
    {
        /// <summary>
        /// TestCaseSource for CompressDifferentFormatsTest
        /// </summary>
        public static List<CompressionMethod> CompressionMethods
        {
            get
            {
                var result = new List<CompressionMethod>();
                foreach(CompressionMethod format in Enum.GetValues(typeof(CompressionMethod)))
                {
                    result.Add(format);
                }

                return result;
            }
        }

        string TempFile()
        {
            return TemporaryFile + "."+ Guid.NewGuid();
        }

        [Test]
        public void CompressFileTest()
        {
            var compressor = new SevenZipCompressor
            {
                ArchiveFormat = OutArchiveFormat.SevenZip,
                DirectoryStructure = false
            };
            var tmpFile = TempFile();
            compressor.CompressFiles(tmpFile, @"Testdata\7z_LZMA2.7z");
            Assert.IsTrue(File.Exists(tmpFile));

            using (var extractor = new SevenZipExtractor(tmpFile))
            {
                extractor.ExtractArchive(OutputDirectory);
            }

            Assert.IsTrue(File.Exists(Path.Combine(OutputDirectory, "7z_LZMA2.7z")));
        }

        [Test]
        public void CompressDirectoryTest()
        {
            var compressor = new SevenZipCompressor
            {
                ArchiveFormat = OutArchiveFormat.SevenZip,
                DirectoryStructure = false
            };
            var tmpFile = TempFile();
            compressor.CompressDirectory("TestData", tmpFile);
            Assert.IsTrue(File.Exists(tmpFile));

            using (var extractor = new SevenZipExtractor(tmpFile))
            {
                extractor.ExtractArchive(OutputDirectory);
            }

            File.Delete(tmpFile);

            Assert.AreEqual(Directory.GetFiles("TestData").Select(Path.GetFileName).ToArray(), Directory.GetFiles(OutputDirectory).Select(Path.GetFileName).ToArray());
        }

        [Test]
        public void CompressWithAppendModeTest()
        {
            var compressor = new SevenZipCompressor
            {
                ArchiveFormat = OutArchiveFormat.SevenZip,
                DirectoryStructure = false
            };
            var tmpFile = TempFile();
            compressor.CompressFiles(tmpFile, @"Testdata\7z_LZMA2.7z");
            Assert.IsTrue(File.Exists(tmpFile));

            using (var extractor = new SevenZipExtractor(tmpFile))
            {
                Assert.AreEqual(1, extractor.FilesCount);
            }

            compressor.CompressionMode = CompressionMode.Append;

            compressor.CompressFiles(tmpFile, @"TestData\zip.zip");

            using (var extractor = new SevenZipExtractor(tmpFile))
            {
                Assert.AreEqual(2, extractor.FilesCount);
            }
        }

        [Test]
        public void ModifyProtectedArchiveTest()
        {
            var compressor = new SevenZipCompressor
            {
                DirectoryStructure = false,
                EncryptHeaders = true
            };
            var tmpFile = TempFile();
            compressor.CompressFilesEncrypted(tmpFile, "password", @"TestData\7z_LZMA2.7z", @"TestData\zip.zip");

            var modificationList = new Dictionary<int, string>
            {
                {0, "changed.zap"},
                {1, null }
            };

            compressor.ModifyArchive(tmpFile, modificationList, "password");

            Assert.IsTrue(File.Exists(tmpFile));

            using (var extractor = new SevenZipExtractor(tmpFile, "password"))
            {
                Assert.AreEqual(1, extractor.FilesCount);
                Assert.AreEqual("changed.zap", extractor.ArchiveFileNames[0]);
            }
        }

        [Test]
        public void CompressWithModifyModeRenameTest()
        {
            var compressor = new SevenZipCompressor
            {
                ArchiveFormat = OutArchiveFormat.SevenZip,
                DirectoryStructure = false
            };
            var tmpFile = TempFile();
            compressor.CompressFiles(tmpFile, @"Testdata\7z_LZMA2.7z");
            Assert.IsTrue(File.Exists(tmpFile));

            compressor.ModifyArchive(tmpFile, new Dictionary<int, string> { { 0, "renamed.7z" }});

            using (var extractor = new SevenZipExtractor(tmpFile))
            {
                Assert.AreEqual(1, extractor.FilesCount);
                extractor.ExtractArchive(OutputDirectory);
            }

            Assert.IsTrue(File.Exists(Path.Combine(OutputDirectory, "renamed.7z")));
            Assert.IsFalse(File.Exists(Path.Combine(OutputDirectory, "7z_LZMA2.7z")));
        }

        [Test]
        public void CompressWithModifyModeDeleteTest()
        {
            var compressor = new SevenZipCompressor
            {
                ArchiveFormat = OutArchiveFormat.SevenZip,
                DirectoryStructure = false
            };
            var tmpFile = TempFile();
            compressor.CompressFiles(tmpFile, @"Testdata\7z_LZMA2.7z");
            Assert.IsTrue(File.Exists(tmpFile));

            compressor.ModifyArchive(tmpFile, new Dictionary<int, string> { { 0, null } });

            using (var extractor = new SevenZipExtractor(tmpFile))
            {
                Assert.AreEqual(0, extractor.FilesCount);
                extractor.ExtractArchive(OutputDirectory);
            }

            Assert.IsFalse(File.Exists(Path.Combine(OutputDirectory, "7z_LZMA2.7z")));
        }

        [Test]
        public void MultiVolumeCompressionTest()
        {
            var compressor = new SevenZipCompressor
            {
                ArchiveFormat = OutArchiveFormat.SevenZip,
                DirectoryStructure = false,
                VolumeSize = 100
            };
            var tmpFile = TempFile();
            compressor.CompressFiles(tmpFile, @"Testdata\7z_LZMA2.7z");

            Assert.AreEqual(3, Directory.GetFiles(OutputDirectory).Length);
            Assert.IsTrue(File.Exists($"{tmpFile}.003"));
        }

        [Test]
        public void CompressToStreamTest()
        {
            var compressor = new SevenZipCompressor {DirectoryStructure = false};
            var tmpFile = TempFile();
            using (var stream = File.Create(tmpFile))
            {
                compressor.CompressFiles(stream, @"TestData\zip.zip");
            }
            
            Assert.IsTrue(File.Exists(tmpFile));

            using (var extractor = new SevenZipExtractor(tmpFile))
            {
                Assert.AreEqual(1, extractor.FilesCount);
                Assert.AreEqual("zip.zip", extractor.ArchiveFileNames[0]);
            }
        }

        [Test]
        public void CompressFromStreamTest()
        {
            var tmpFile = TempFile();
            using (var input = File.OpenRead(@"TestData\zip.zip"))
            {
                using (var output = File.Create(tmpFile))
                {
                    var compressor = new SevenZipCompressor
                    {
                        DirectoryStructure = false
                    };

                    compressor.CompressStream(input, output);
                }
                    
            }

            Assert.IsTrue(File.Exists(tmpFile));

            using (var extractor = new SevenZipExtractor(tmpFile))
            {
                Assert.AreEqual(1, extractor.FilesCount);
                Assert.AreEqual(new FileInfo(@"TestData\zip.zip").Length, extractor.ArchiveFileData[0].Size);
            }
        }

        [Test]
        public void CompressFileDictionaryTest()
        {
            var compressor = new SevenZipCompressor { DirectoryStructure = false };

            var fileDict = new Dictionary<string, string>
            {
                {"zip.zip", @"TestData\zip.zip"}
            };
            var tmpFile = TempFile();
            compressor.CompressFileDictionary(fileDict, tmpFile);

            Assert.IsTrue(File.Exists(tmpFile));

            using (var extractor = new SevenZipExtractor(tmpFile))
            {
                Assert.AreEqual(1, extractor.FilesCount);
                Assert.AreEqual("zip.zip", extractor.ArchiveFileNames[0]);
            }
        }

        [Test]
        public void ThreadedCompressionTest()
        {
			var tempFile1 = Path.Combine(OutputDirectory, "t1.7z");
			var tempFile2 = Path.Combine(OutputDirectory, "t2.7z");

			var t1 = new Thread(() =>
            {
                var tmp = new SevenZipCompressor();
				tmp.CompressDirectory("TestData", tempFile1);
			});

            var t2 = new Thread(() =>
            {
                var tmp = new SevenZipCompressor();                
                tmp.CompressDirectory("TestData", tempFile2);
            });

            t1.Start();
            t2.Start();
            t1.Join();
            t2.Join();

			Assert.IsTrue(File.Exists(tempFile1));
			Assert.IsTrue(File.Exists(tempFile2));
		}

        [Test, TestCaseSource(nameof(CompressionMethods))]
        public void CompressDifferentFormatsTest(CompressionMethod method)
        {
            var compressor = new SevenZipCompressor
            {
                ArchiveFormat = OutArchiveFormat.SevenZip,
                CompressionMethod = method
            };
            var tmpFile = TempFile();
            compressor.CompressFiles(tmpFile, @"TestData\zip.zip");

            Assert.IsTrue(File.Exists(tmpFile));
        }
    }
}
