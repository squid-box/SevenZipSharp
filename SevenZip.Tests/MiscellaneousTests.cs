namespace SevenZip.Tests
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Runtime.Serialization.Formatters.Binary;
    using NUnit.Framework;

    using SevenZip;

    [TestFixture]
    public class MiscellaneousTests : TestBase
    {
        [Test]
        public void SerializationTest()
        {
            var argumentException = new ArgumentException("blahblah");
            var binaryFormatter = new BinaryFormatter();

            using (var ms = new MemoryStream())
            {
                using (var fileStream = File.Create(TemporaryFile))
                {
                    binaryFormatter.Serialize(ms, argumentException);
                    var compressor = new SevenZipCompressor();
                    compressor.CompressStream(ms, fileStream);
                }
            }
        }

#if SFX
        [Test]
        public void CreateSfxArchiveTest([Values]SfxModule sfxModule)
        {
            if (sfxModule.HasFlag(SfxModule.Custom))
            {
                Assert.Ignore("No idea how to use SfxModule \"Custom\".");
            }

            var sfxFile = Path.Combine(OutputDirectory, "sfx.exe");
            var sfx = new SevenZipSfx(sfxModule);
            var compressor = new SevenZipCompressor {DirectoryStructure = false};

            compressor.CompressFiles(TemporaryFile, @"TestData\zip.zip");

            sfx.MakeSfx(TemporaryFile, sfxFile);

            Assert.IsTrue(File.Exists(sfxFile));

            using (var extractor = new SevenZipExtractor(sfxFile))
            {
                Assert.AreEqual(1, extractor.FilesCount);
                Assert.AreEqual("zip.zip", extractor.ArchiveFileNames[0]);
            }

            if (sfxModule == SfxModule.Installer)
            {
                // Installer modules need to be run with elevation.
                Assert.Pass("Assume SFX installer works...");
                return;
            }

            Assert.DoesNotThrow(() =>
            {
                var process = Process.Start(sfxFile);
                process?.Kill();
            });
        }
#endif

        [Test]
        public void LzmaEncodeDecodeTest()
        {
            using (var output = new FileStream(TemporaryFile, FileMode.Create))
            {
                var encoder = new LzmaEncodeStream(output);
                using (var inputSample = new FileStream(@"TestData\zip.zip", FileMode.Open))
                {
                    int bufSize = 24576, count;
                    var buf = new byte[bufSize];

                    while ((count = inputSample.Read(buf, 0, bufSize)) > 0)
                    {
                        encoder.Write(buf, 0, count);
                    }
                }

                encoder.Close();
            }

            var newZip = Path.Combine(OutputDirectory, "new.zip");

            using (var input = new FileStream(TemporaryFile, FileMode.Open))
            {
                var decoder = new LzmaDecodeStream(input);
                using (var output = new FileStream(newZip, FileMode.Create))
                {

                    int bufSize = 24576, count;
                    var buf = new byte[bufSize];

                    while ((count = decoder.Read(buf, 0, bufSize)) > 0)
                    {
                        output.Write(buf, 0, count);
                    }
                }
            }

            Assert.IsTrue(File.Exists(newZip));

            using (var extractor = new SevenZipExtractor(newZip))
            {

                Assert.AreEqual(1, extractor.FilesCount);
                Assert.AreEqual("zip.txt", extractor.ArchiveFileNames[0]);
            }
        }
    }
}
