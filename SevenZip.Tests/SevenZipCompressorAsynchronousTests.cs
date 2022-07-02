namespace SevenZip.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using NUnit.Framework;

    [TestFixture]
    public class SevenZipCompressorAsynchronousTests : TestBase
    {
        [Test]
        public void AsynchronousCompressDirectoryAndEventsTest()
        {
            var filesFoundInvoked = 0;
            var fileCompressionStartedInvoked = 0;
            var fileCompressionFinishedInvoked = 0;
            var compressingInvoked = 0;
            var compressionFinishedInvoked = 0;

            var compressor = new SevenZipCompressor();

            compressor.FilesFound += (o, e) => filesFoundInvoked++;
            compressor.FileCompressionStarted += (o, e) => fileCompressionStartedInvoked++;
            compressor.FileCompressionFinished += (o, e) => fileCompressionFinishedInvoked++;
            compressor.Compressing += (o, e) => compressingInvoked++;
            compressor.CompressionFinished += (o, e) => compressionFinishedInvoked++;

            compressor.BeginCompressDirectory(@"TestData", TemporaryFile);

            var timeToWait = 1000;
            while (compressionFinishedInvoked == 0)
            {
                if (timeToWait <= 0)
                {
                    break;
                }

                Thread.Sleep(25);
                timeToWait -= 25;
            }

            var numberOfTestDataFiles = Directory.GetFiles("TestData").Length;

            Assert.AreEqual(1, filesFoundInvoked);
            Assert.AreEqual(numberOfTestDataFiles, fileCompressionStartedInvoked);
            Assert.AreEqual(numberOfTestDataFiles, fileCompressionFinishedInvoked);
            Assert.AreEqual(numberOfTestDataFiles, compressingInvoked);
            Assert.AreEqual(1, compressionFinishedInvoked);

            Assert.IsTrue(File.Exists(TemporaryFile));
        }

        [Test]
        public void AsynchronousCompressFilesTest()
        {
            var compressionFinishedInvoked = false;

            var compressor = new SevenZipCompressor { DirectoryStructure = false };
            compressor.CompressionFinished += (o, e) => compressionFinishedInvoked = true;

            compressor.BeginCompressFiles(TemporaryFile, @"TestData\zip.zip", @"TestData\tar.tar");

            var timeToWait = 1000;
            while (!compressionFinishedInvoked)
            {
                if (timeToWait <= 0)
                {
                    break;
                }

                Thread.Sleep(25);
                timeToWait -= 25;
            }

            Assert.IsTrue(File.Exists(TemporaryFile));

            using (var extractor = new SevenZipExtractor(TemporaryFile))
            {
                Assert.AreEqual(2, extractor.FilesCount);
                Assert.IsTrue(extractor.ArchiveFileNames.Contains("zip.zip"));
                Assert.IsTrue(extractor.ArchiveFileNames.Contains("tar.tar"));
            }
        }

        [Test]
        public void AsynchronousCompressStreamTest()
        {
            var compressionFinishedInvoked = false;

            var compressor = new SevenZipCompressor { DirectoryStructure = false };
            compressor.CompressionFinished += (o, e) => compressionFinishedInvoked = true;

            using (var inputStream = File.OpenRead(@"TestData\zip.zip"))
            {
                using (var outputStream = new FileStream(TemporaryFile, FileMode.Create))
                {
                    compressor.BeginCompressStream(inputStream, outputStream);

                    var timeToWait = 1000;
                    while (!compressionFinishedInvoked)
                    {
                        if (timeToWait <= 0)
                        {
                            break;
                        }

                        Thread.Sleep(25);
                        timeToWait -= 25;
                    }
                }
            }

            Assert.IsTrue(File.Exists(TemporaryFile));

            using (var extractor = new SevenZipExtractor(TemporaryFile))
            {
                Assert.AreEqual(1, extractor.FilesCount);
            }
        }

        [Test]
        public void AsynchronousModifyArchiveTest()
        {
            var compressor = new SevenZipCompressor { DirectoryStructure = false };

            compressor.CompressFiles(TemporaryFile, @"TestData\tar.tar");

            var compressionFinishedInvoked = false;
            compressor.CompressionFinished += (o, e) => compressionFinishedInvoked = true;

            compressor.BeginModifyArchive(TemporaryFile, new Dictionary<int, string> { { 0, @"tartar" } });

            var timeToWait = 1000;
            while (!compressionFinishedInvoked)
            {
                if (timeToWait <= 0)
                {
                    break;
                }

                Thread.Sleep(25);
                timeToWait -= 25;
            }

            Assert.IsTrue(File.Exists(TemporaryFile));

            using (var extractor = new SevenZipExtractor(TemporaryFile))
            {
                Assert.AreEqual(1, extractor.FilesCount);
                Assert.AreEqual("tartar", extractor.ArchiveFileNames[0]);
            }
        }

        [Test]
        public void AsynchronousCompressFilesEncryptedTest()
        {
            var compressionFinishedInvoked = false;

            var compressor = new SevenZipCompressor { DirectoryStructure = false };
            compressor.CompressionFinished += (o, e) => compressionFinishedInvoked = true;

            compressor.BeginCompressFilesEncrypted(TemporaryFile, "secure", @"TestData\zip.zip", @"TestData\tar.tar");

            var timeToWait = 1000;
            while (!compressionFinishedInvoked)
            {
                if (timeToWait <= 0)
                {
                    break;
                }

                Thread.Sleep(25);
                timeToWait -= 25;
            }

            Assert.IsTrue(File.Exists(TemporaryFile));

            using (var extractor = new SevenZipExtractor(TemporaryFile))
            {
                Assert.AreEqual(2, extractor.FilesCount);
                Assert.IsTrue(extractor.ArchiveFileNames.Contains("zip.zip"));
                Assert.IsTrue(extractor.ArchiveFileNames.Contains("tar.tar"));

                Assert.Throws<ExtractionFailedException>(() => extractor.ExtractArchive(OutputDirectory));
            }
        }

        [Test]
        public async Task CompressFilesAsync()
        {
            var compressor = new SevenZipCompressor { DirectoryStructure = false };
            await compressor.CompressFilesAsync(TemporaryFile, @"TestData\zip.zip", @"TestData\tar.tar");

            Assert.IsTrue(File.Exists(TemporaryFile));

            using (var extractor = new SevenZipExtractor(TemporaryFile))
            {
                Assert.AreEqual(2, extractor.FilesCount);
                Assert.IsTrue(extractor.ArchiveFileNames.Contains("zip.zip"));
                Assert.IsTrue(extractor.ArchiveFileNames.Contains("tar.tar"));
            }
        }

        [Test]
        public void CompressFilesAsyncWithCancellationTest()
        {
            var compressor = new SevenZipCompressor { DirectoryStructure = false };
            compressor.Compressing += Compressor_Compressing;
            var task = compressor.CompressFilesAsync(TemporaryFile, @"TestData\zip.zip", @"TestData\tar.tar");

            Assert.ThrowsAsync<SevenZipCompressionCanceledException>(async () => { await task; });            
        }

        private void Compressor_Compressing(object sender, ProgressEventArgs e)
        {
            if (e.PercentDone > 50)
            {
                e.Cancel = true;
            }
        }

        [Test]
        public async Task CompressDirectoryAsync()
        {
            var compressor = new SevenZipCompressor { DirectoryStructure = false };
            await compressor.CompressDirectoryAsync("TestData", TemporaryFile);

            Assert.IsTrue(File.Exists(TemporaryFile));

            using (var extractor = new SevenZipExtractor(TemporaryFile))
            {
                Assert.AreEqual(Directory.GetFiles("TestData").Length, extractor.FilesCount);
                Assert.IsTrue(extractor.ArchiveFileNames.Contains("zip.zip"));
                Assert.IsTrue(extractor.ArchiveFileNames.Contains("tar.tar"));
            }
        }

        [Test]
        public async Task CompressFilesEncryptedAsync()
        {
            var compressor = new SevenZipCompressor { DirectoryStructure = false };
            await compressor.CompressFilesEncryptedAsync(TemporaryFile, "secure", @"TestData\zip.zip", @"TestData\tar.tar");

            Assert.IsTrue(File.Exists(TemporaryFile));

            using (var extractor = new SevenZipExtractor(TemporaryFile, "insecure"))
            {
                Assert.AreEqual(2, extractor.FilesCount);
                Assert.IsTrue(extractor.ArchiveFileNames.Contains("zip.zip"));
                Assert.IsTrue(extractor.ArchiveFileNames.Contains("tar.tar"));

                Assert.Throws<ExtractionFailedException>(() => extractor.ExtractArchive(OutputDirectory));
            }
        }
    }
}
