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
    public class SevenZipExtractorTests : TestBase
    {
        public static List<TestFile> TestFiles
        {
            get
            {
                var result = new List<TestFile>();

                foreach (var file in Directory.GetFiles(Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData")))
                {
                    if (file.Contains("multi") || file.Contains("long_path"))
                    {
                        continue;
                    }

                    result.Add(new TestFile(file));
                }

                return result;
            }
        }
        /// <summary>
        /// TestCaseSource for ReadSolidArchive test
        /// </summary>
        public static List<TestFile> SolidArchivesFiles
        {
            get
            {
                var result = new List<TestFile>();
                foreach (var file in Directory.GetFiles(Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData_solid")))
                {
                    result.Add(new TestFile(file));
                }

                return result;
            }
        }

        [Test]
        public void ExtractFilesTest()
        {
            using (var extractor = new SevenZipExtractor(@"TestData\multiple_files.7z"))
            {
                for (var i = 0; i < extractor.ArchiveFileData.Count; i++)
                {
                    extractor.ExtractFiles(OutputDirectory, extractor.ArchiveFileData[i].Index);
                }

                Assert.AreEqual(3, Directory.GetFiles(OutputDirectory).Length);
            }
        }

        [Test]
        public void ExtractSpecificFilesTest()
        {
            using (var extractor = new SevenZipExtractor(@"TestData\multiple_files.7z"))
            {
                extractor.ExtractFiles(OutputDirectory, 0, 2);
                Assert.AreEqual(2, Directory.GetFiles(OutputDirectory).Length);
            }

            Assert.AreEqual(2, Directory.GetFiles(OutputDirectory).Length);
            Assert.Contains(Path.Combine(OutputDirectory, "file1.txt"), Directory.GetFiles(OutputDirectory));
            Assert.Contains(Path.Combine(OutputDirectory, "file3.txt"), Directory.GetFiles(OutputDirectory));
        }

        [Test]
        public void ExtractArchiveMultiVolumesTest()
        {
            using (var extractor = new SevenZipExtractor(@"TestData\multivolume.part0001.rar"))
            {
                extractor.ExtractArchive(OutputDirectory);
            }

            Assert.AreEqual(1, Directory.GetFiles(OutputDirectory).Length);
            Assert.IsTrue(File.ReadAllText(Directory.GetFiles(OutputDirectory)[0]).StartsWith("Lorem ipsum dolor sit amet"));
        }

        [Test]
        public void ExtractionWithCancellationTest()
        {
            using (var tmp = new SevenZipExtractor(@"TestData\multiple_files.7z"))
            {
                tmp.FileExtractionStarted += (s, e) =>
                {
                    if (e.FileInfo.Index == 2)
                    {
                        e.Cancel = true;
                    }
                };

                tmp.ExtractArchive(OutputDirectory);

                Assert.AreEqual(2, Directory.GetFiles(OutputDirectory).Length);
            }
        }

        [Test]
        public void ExtractionWithSkipTest()
        {
            using (var tmp = new SevenZipExtractor(@"TestData\multiple_files.7z"))
            {
                tmp.FileExtractionStarted += (s, e) =>
                                             {
                                                 if (e.FileInfo.Index == 1)
                                                 {
                                                     e.Skip = true;
                                                 }
                                             };

                tmp.ExtractArchive(OutputDirectory);

                Assert.AreEqual(2, Directory.GetFiles(OutputDirectory).Length);
            }
        }

        [Test]
        public void ExtractionFromStreamTest()
        {
            // TODO: Rewrite this to test against more/all TestData archives.

            using (var tmp = new SevenZipExtractor(File.OpenRead(@"TestData\multiple_files.7z")))
            {
                tmp.ExtractArchive(OutputDirectory);
                Assert.AreEqual(3, Directory.GetFiles(OutputDirectory).Length);
            }
        }

        [Test]
        public void ExtractionToStreamTest()
        {
            using (var tmp = new SevenZipExtractor(@"TestData\multiple_files.7z"))
            {
                using (var fileStream = new FileStream(Path.Combine(OutputDirectory, "streamed_file.txt"), FileMode.Create))
                {
                    tmp.ExtractFile(1, fileStream);
                }
            }

            Assert.AreEqual(1, Directory.GetFiles(OutputDirectory).Length);

            var extractedFile = Directory.GetFiles(OutputDirectory)[0];

            Assert.AreEqual("file2", File.ReadAllText(extractedFile));
        }

        [Test]
        public void DetectMultiVolumeIndexTest()
        {
            using (var tmp = new SevenZipExtractor(@"TestData\multivolume.part0001.rar"))
            {
                Assert.IsTrue(tmp.ArchiveProperties.Any(x => x.Name.Equals("IsVolume") && x.Value != null && x.Value.Equals(true)));
                Assert.IsTrue(tmp.ArchiveProperties.Any(x => x.Name.Equals("VolumeIndex") && x.Value != null && Convert.ToInt32(x.Value) == 0));
            }

            using (var tmp = new SevenZipExtractor(@"TestData\multivolume.part0002.rar"))
            {
                Assert.IsTrue(tmp.ArchiveProperties.Any(x => x.Name.Equals("IsVolume") && x.Value != null && x.Value.Equals(true)));
                Assert.IsFalse(tmp.ArchiveProperties.Any(x => x.Name.Equals("VolumeIndex") && x.Value != null && Convert.ToInt32(x.Value) == 0));
            }
        }

        [Test]
        public void ThreadedExtractionTest()
        {
            var destination1 = Path.Combine(OutputDirectory, "t1");
            var destination2 = Path.Combine(OutputDirectory, "t2");

            var t1 = new Thread(() =>
            {
                using (var tmp = new SevenZipExtractor(@"TestData\multiple_files.7z"))
                {
                    tmp.ExtractArchive(destination1);
                }
            });
            var t2 = new Thread(() =>
            {
                using (var tmp = new SevenZipExtractor(@"TestData\multiple_files.7z"))
                {
                    tmp.ExtractArchive(destination2);
                }
            });

            t1.Start();
            t2.Start();
            t1.Join();
            t2.Join();

            Assert.IsTrue(Directory.Exists(destination1));
            Assert.IsTrue(Directory.Exists(destination2));
            Assert.AreEqual(3, Directory.GetFiles(destination1).Length);
            Assert.AreEqual(3, Directory.GetFiles(destination2).Length);
        }

        [Test]
        public void ExtractArchiveWithLongPath()
        {
            using (var extractor = new SevenZipExtractor(@"TestData\long_path.7z"))
            {
                Assert.Throws<PathTooLongException>(() => extractor.ExtractArchive(OutputDirectory));
            }
        }

        [Test]
        public void ReadArchivedFileNames()
        {
            using (var extractor = new SevenZipExtractor(@"TestData\multiple_files.7z"))
            {
                var fileNames = extractor.ArchiveFileNames;
                Assert.AreEqual(3, fileNames.Count);

                Assert.AreEqual("file1.txt", fileNames[0]);
                Assert.AreEqual("file2.txt", fileNames[1]);
                Assert.AreEqual("file3.txt", fileNames[2]);
            }
        }

        [Test]
        public void ReadArchivedFileData()
        {
            using (var extractor = new SevenZipExtractor(@"TestData\multiple_files.7z"))
            {
                var fileData = extractor.ArchiveFileData;
                Assert.AreEqual(3, fileData.Count);

                Assert.AreEqual("file1.txt", fileData[0].FileName);
                Assert.IsFalse(fileData[0].Encrypted);
                Assert.IsFalse(fileData[0].IsDirectory);
            }
        }

        [Test, TestCaseSource(nameof(TestFiles))]
        public void ExtractDifferentFormatsTest(TestFile file)
        {
            using (var extractor = new SevenZipExtractor(file.FilePath))
            {
                extractor.ExtractArchive(OutputDirectory);
                Assert.AreEqual(1, Directory.GetFiles(OutputDirectory).Length);
            }
        }

        [Test, TestCaseSource(nameof(SolidArchivesFiles))]
        public void ReadStreamFromSolidArchiveTest(TestFile file)
        {
            using (var extractor = new SevenZipExtractor(file.FilePath))
            {
                bool isSolid = Path.GetFileName(file.FilePath).ToLowerInvariant().Contains("solid");

                var fileData = extractor.ArchiveFileData;
                Assert.AreEqual(isSolid, extractor.IsSolid);
                Assert.AreEqual(5, fileData.Count);

                // extract files in non-sequential order
                foreach (int index in new int[] { 2, 4, 1, 3, 0 })
                {
                    string content = string.Empty;
                    using (var stream = new MemoryStream())
                    {
                        extractor.ExtractFile(4, stream);
                        stream.Seek(0, SeekOrigin.Begin);

                        using (TextReader reader = new StreamReader(stream))
                        {
                            content = reader.ReadToEnd();
                        }
                    }
                    Assert.AreEqual("test file", content);
                }
            }
        }

        [Test, TestCaseSource(nameof(SolidArchivesFiles))]
        public void ExtractSpecificFilesFromSolidArchiveTest(TestFile file)
        {
            using (var extractor = new SevenZipExtractor(file.FilePath))
            {
                extractor.ExtractFiles(OutputDirectory, 2, 4);
                Assert.AreEqual(2, Directory.GetFiles(OutputDirectory).Length);
            }

            Assert.AreEqual(2, Directory.GetFiles(OutputDirectory).Length);
            Assert.Contains(Path.Combine(OutputDirectory, "test3.txt"), Directory.GetFiles(OutputDirectory));
            Assert.Contains(Path.Combine(OutputDirectory, "test5.txt"), Directory.GetFiles(OutputDirectory));
        }

        [Test, TestCaseSource(nameof(SolidArchivesFiles))]
        public void ExtractFileCallbackFromSolidArchiveTest(TestFile file)
        {
            using (var extractor = new SevenZipExtractor(file.FilePath))
            {
                extractor.ExtractFiles(args =>
                {
                    if (args.Reason == ExtractFileCallbackReason.Start &&
                        (args.ArchiveFileInfo.Index == 2 || args.ArchiveFileInfo.Index == 4))
                    {
                        args.ExtractToFile = Path.Combine(OutputDirectory, args.ArchiveFileInfo.FileName);
                    }
                });
            }

            Assert.AreEqual(2, Directory.GetFiles(OutputDirectory).Length);
            Assert.Contains(Path.Combine(OutputDirectory, "test3.txt"), Directory.GetFiles(OutputDirectory));
            Assert.Contains(Path.Combine(OutputDirectory, "test5.txt"), Directory.GetFiles(OutputDirectory));
        }

        [Test, TestCaseSource(nameof(SolidArchivesFiles))]
        public void ReadStreamCallbackFromSolidArchiveTest(TestFile file)
        {
            using (var extractor = new SevenZipExtractor(file.FilePath))
            {
                extractor.ExtractFiles(args =>
                {
                    if (args.Reason == ExtractFileCallbackReason.Start)
                    {
                        if (args.ArchiveFileInfo.Index == 2 || args.ArchiveFileInfo.Index == 4)
                        {
                            args.ExtractToStream = new MemoryStream();
                        }
                    }
                    else if (args.Reason == ExtractFileCallbackReason.Done)
                    {
                        if (args.ExtractToStream != null)
                        {
                            string content = string.Empty;
                            args.ExtractToStream.Seek(0, SeekOrigin.Begin);
                            using (TextReader reader = new StreamReader(args.ExtractToStream))
                            {
                                content = reader.ReadToEnd();
                            }
                            Assert.AreEqual("test file", content);

                            args.ExtractToStream.Dispose();
                            args.ExtractToStream = null;
                        }
                    }
                    else
                    {
                        Assert.Fail("Extract file failed " + args.Exception?.Message);
                    }
                });
            }
        }
    }

    /// <summary>
    /// Simple wrapper to get better names for ExtractDifferentFormatsTest results.
    /// </summary>
    public class TestFile
    {
        public string FilePath { get; }

        public TestFile(string filePath)
        {
            FilePath = filePath;
        }

        public override string ToString()
        {
            return Path.GetFileName(FilePath);
        }
    }
}
