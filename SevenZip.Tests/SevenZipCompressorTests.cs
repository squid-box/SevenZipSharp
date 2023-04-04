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

        [Test]
        public void CompressDirectory_WithSfnPath()
        {
            var compressor = new SevenZipCompressor
            {
                ArchiveFormat = OutArchiveFormat.Zip,
                PreserveDirectoryRoot = true
            };

            compressor.CompressDirectory("TESTDA~1", TemporaryFile);
            Assert.IsTrue(File.Exists(TemporaryFile));

            using (var extractor = new SevenZipExtractor(TemporaryFile))
            {
                Assert.AreEqual(1, extractor.FilesCount);
                Assert.IsTrue(extractor.ArchiveFileNames[0].StartsWith("TestData_LongerDirectoryName", StringComparison.OrdinalIgnoreCase));
            }
        }

        [Test]
        public void CompressDirectory_NonExistentDirectory()
        {
            var compressor = new SevenZipCompressor();

            Assert.Throws<ArgumentException>(() => compressor.CompressDirectory("nonexistent", TemporaryFile));
            Assert.Throws<ArgumentException>(() => compressor.CompressDirectory("", TemporaryFile));
        }

        [Test]
        public void CompressFile_WithSfnPath()
        {
            var compressor = new SevenZipCompressor
            {
                ArchiveFormat = OutArchiveFormat.Zip
            };

            compressor.CompressFiles(TemporaryFile, @"TESTDA~1\emptyfile.txt");
            Assert.IsTrue(File.Exists(TemporaryFile));

            using (var extractor = new SevenZipExtractor(TemporaryFile))
            {
                Assert.AreEqual(1, extractor.FilesCount);
                Assert.AreEqual("emptyfile.txt", extractor.ArchiveFileNames[0]);
            }
        }

        [Test]
        public void CompressFileTest()
        {
            var compressor = new SevenZipCompressor
            {
                ArchiveFormat = OutArchiveFormat.SevenZip,
                DirectoryStructure = false
            };
            
            compressor.CompressFiles(TemporaryFile, @"Testdata\7z_LZMA2.7z");
            Assert.IsTrue(File.Exists(TemporaryFile));

            using (var extractor = new SevenZipExtractor(TemporaryFile))
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

            compressor.CompressDirectory("TestData", TemporaryFile);
            Assert.IsTrue(File.Exists(TemporaryFile));

            using (var extractor = new SevenZipExtractor(TemporaryFile))
            {
                extractor.ExtractArchive(OutputDirectory);
            }

            File.Delete(TemporaryFile);

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

            compressor.CompressFiles(TemporaryFile, @"Testdata\7z_LZMA2.7z");
            Assert.IsTrue(File.Exists(TemporaryFile));

            using (var extractor = new SevenZipExtractor(TemporaryFile))
            {
                Assert.AreEqual(1, extractor.FilesCount);
            }

            compressor.CompressionMode = CompressionMode.Append;

            compressor.CompressFiles(TemporaryFile, @"TestData\zip.zip");

            using (var extractor = new SevenZipExtractor(TemporaryFile))
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

            compressor.CompressFilesEncrypted(TemporaryFile, "password", @"TestData\7z_LZMA2.7z", @"TestData\zip.zip");

            var modificationList = new Dictionary<int, string>
            {
                {0, "changed.zap"},
                {1, null }
            };

            compressor.ModifyArchive(TemporaryFile, modificationList, "password");

            Assert.IsTrue(File.Exists(TemporaryFile));

            using (var extractor = new SevenZipExtractor(TemporaryFile, "password"))
            {
                Assert.AreEqual(1, extractor.FilesCount);
                Assert.AreEqual("changed.zap", extractor.ArchiveFileNames[0]);
            }
        }

        [Test]
        public void ModifyNonArchiveTest()
        {
            var compressor = new SevenZipCompressor
            {
                DirectoryStructure = false
            };

            File.WriteAllText(TemporaryFile, "I'm not an archive.");

            var modificationList = new Dictionary<int, string> {{0, ""}};

            Assert.Throws<SevenZipArchiveException>(() => compressor.ModifyArchive(TemporaryFile, modificationList));
        }

        [Test]
        public void CompressWithModifyModeRenameTest()
        {
            var compressor = new SevenZipCompressor
            {
                ArchiveFormat = OutArchiveFormat.SevenZip,
                DirectoryStructure = false
            };

            compressor.CompressFiles(TemporaryFile, @"Testdata\7z_LZMA2.7z");
            Assert.IsTrue(File.Exists(TemporaryFile));

            compressor.ModifyArchive(TemporaryFile, new Dictionary<int, string> { { 0, "renamed.7z" }});

            using (var extractor = new SevenZipExtractor(TemporaryFile))
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

            compressor.CompressFiles(TemporaryFile, @"Testdata\7z_LZMA2.7z");
            Assert.IsTrue(File.Exists(TemporaryFile));

            compressor.ModifyArchive(TemporaryFile, new Dictionary<int, string> { { 0, null } });

            using (var extractor = new SevenZipExtractor(TemporaryFile))
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

            compressor.CompressFiles(TemporaryFile, @"Testdata\7z_LZMA2.7z");

            Assert.AreEqual(3, Directory.GetFiles(OutputDirectory).Length);
            Assert.IsTrue(File.Exists($"{TemporaryFile}.003"));
        }

        [Test]
        public void CompressToStreamTest()
        {
            var compressor = new SevenZipCompressor {DirectoryStructure = false};

            using (var stream = File.Create(TemporaryFile))
            {
                compressor.CompressFiles(stream, @"TestData\zip.zip");
            }
            
            Assert.IsTrue(File.Exists(TemporaryFile));

            using (var extractor = new SevenZipExtractor(TemporaryFile))
            {
                Assert.AreEqual(1, extractor.FilesCount);
                Assert.AreEqual("zip.zip", extractor.ArchiveFileNames[0]);
            }
        }

        [Test]
        public void CompressFromStreamTest()
        {
            using (var input = File.OpenRead(@"TestData\zip.zip"))
            {
                using (var output = File.Create(TemporaryFile))
                {
                    var compressor = new SevenZipCompressor
                    {
                        DirectoryStructure = false
                    };

                    compressor.CompressStream(input, output);
                }
                    
            }

            Assert.IsTrue(File.Exists(TemporaryFile));

            using (var extractor = new SevenZipExtractor(TemporaryFile))
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

            compressor.CompressFileDictionary(fileDict, TemporaryFile);

            Assert.IsTrue(File.Exists(TemporaryFile));

            using (var extractor = new SevenZipExtractor(TemporaryFile))
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

            compressor.CompressFiles(TemporaryFile, @"TestData\zip.zip");

            Assert.IsTrue(File.Exists(TemporaryFile));
        }

        [Test]
        public void AppendToArchiveWithEncryptedHeadersTest()
        {
            var compressor = new SevenZipCompressor()
            {
                ArchiveFormat = OutArchiveFormat.SevenZip,
                CompressionMethod = CompressionMethod.Lzma2,
                CompressionLevel = CompressionLevel.Normal,
                EncryptHeaders = true,
            };
            compressor.CompressDirectory(@"TestData", TemporaryFile, "password");

            compressor = new SevenZipCompressor
            {
                CompressionMode = CompressionMode.Append
            };

            compressor.CompressFilesEncrypted(TemporaryFile, "password", @"TestData\zip.zip");
        }

        [Test]
        public void AppendEncryptedFileToStreamTest()
        {
            using (var fileStream = new FileStream(TemporaryFile, FileMode.Create))
            {
                var compressor = new SevenZipCompressor
                {
                    ArchiveFormat = OutArchiveFormat.SevenZip,
                    CompressionMethod = CompressionMethod.Lzma2,
                    CompressionMode = CompressionMode.Append,
                    ZipEncryptionMethod = ZipEncryptionMethod.Aes256,
                    CompressionLevel = CompressionLevel.Normal,
                    EncryptHeaders = true
                };

                compressor.CompressFilesEncrypted(fileStream, "password", @"TestData\zip.zip");
            }

            using (var extractor = new SevenZipExtractor(TemporaryFile, "password"))
            {
                Assert.AreEqual(1, extractor.FilesCount);
                Assert.AreEqual("zip.zip", extractor.ArchiveFileNames[0]);
            }
        }
    }
}
