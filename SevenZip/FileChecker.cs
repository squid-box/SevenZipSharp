namespace SevenZip
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;

#if UNMANAGED
    /// <summary>
    /// The signature checker class. Original code by Siddharth Uppal, adapted by Markhor.
    /// </summary>
    /// <remarks>Based on the code at http://blog.somecreativity.com/2008/04/08/how-to-check-if-a-file-is-compressed-in-c/#</remarks>
    internal static class FileChecker
    {
        private const int SIGNATURE_SIZE = 21;
        private const int SFX_SCAN_LENGTH = 256 * 1024;

        /// <summary>
        /// Gets the InArchiveFormat for a specific extension.
        /// </summary>
        /// <param name="stream">The stream to identify.</param>
        /// <param name="offset">The archive beginning offset.</param>
        /// <param name="isExecutable">True if the original format of the stream is PE; otherwise, false.</param>
        /// <returns>Corresponding InArchiveFormat.</returns>
        public static InArchiveFormat CheckSignature(Stream stream, out int offset, out bool isExecutable)
        {
            return CheckSignature((InArchiveFormat)(-1), stream, out offset, out isExecutable);
        }

        /// <summary>
        /// Gets the InArchiveFormat for a specific extension.
        /// </summary>
        /// <param name="formatByFileName">The InArchiveFormat that was detected by the extension of the filename.</param>
        /// <param name="stream">The stream to identify.</param>
        /// <param name="offset">The archive beginning offset.</param>
        /// <param name="isExecutable">True if the original format of the stream is PE; otherwise, false.</param>
        /// <returns>Corresponding InArchiveFormat.</returns>
        private static InArchiveFormat CheckSignature(InArchiveFormat formatByFileName,
                                                      Stream          stream,
                                                      out int         offset,
                                                      out bool        isExecutable)
        {
            offset = 0;
            isExecutable = false;

            if (!stream.CanRead)
            {
                throw new ArgumentException("The stream must be readable.");
            }

            if (!stream.CanSeek)
            {
                throw new ArgumentException("The stream must be seekable.");
            }

            var formatsByData = new SortedList<int, InArchiveFormat>(new DuplicateKeyComparer<int>(true));

            foreach (var format in new[]
            {
                InArchiveFormat.Arj,
                InArchiveFormat.BZip2,
                InArchiveFormat.Cab,
                InArchiveFormat.Chm,
                InArchiveFormat.Deb,
                InArchiveFormat.Dmg,
                InArchiveFormat.Elf,
                InArchiveFormat.Flv,
                InArchiveFormat.GZip,
                InArchiveFormat.Hfs,
                InArchiveFormat.Iso,
                InArchiveFormat.Lzh,
                InArchiveFormat.Lzma,
                InArchiveFormat.Lzw,
                InArchiveFormat.Mub,
                InArchiveFormat.PE,
                InArchiveFormat.Rar,
                InArchiveFormat.Rar4,
                InArchiveFormat.Rpm,
                InArchiveFormat.SevenZip,
                InArchiveFormat.Swf,
                InArchiveFormat.Tar,
                InArchiveFormat.Udf,
                InArchiveFormat.Vhd,
                InArchiveFormat.Wim,
                InArchiveFormat.XZ,
                InArchiveFormat.Xar,
                InArchiveFormat.Zip,
            })
            {
                int resultQuality = CheckSignatureIsFormat(format, stream, 0);
                if (resultQuality > 0)
                    formatsByData.Add(resultQuality, format);
            }

            var detectedFormat = (InArchiveFormat)(-1);

            if (formatsByData.Count == 0)
            {
                detectedFormat = formatByFileName;
            }
            else if (formatsByData.Count == 1)
            {
                // Even if the output is always "detectedFormat = formatByData", still keep all these if-else paths because they explain the criteria of the decisions.
                // It may be simplified again if the detection of the formatByData has been made more robust and reliable.
                var formatByData = formatsByData.Values[0];
                if (formatByData == formatByFileName           // Use found format-by-data if it matches the format-by-filename
                 || formatByFileName == (InArchiveFormat)(-1)) // or if a match is impossible because no format-by-filename exists.
                {
                    detectedFormat = formatByData;
                }
                else if (formatByData == InArchiveFormat.Rar4
                      && formatByFileName == InArchiveFormat.Rar)
                {
                    detectedFormat = formatByData;
                }
                else // The format-by-data does not match format-by-filename. The file extension may be wrong, therefore the format-by-data is used.
                {
                    detectedFormat = formatByData;
                }
            }
            else // more than one match
            {
                if (formatsByData.ContainsValue(InArchiveFormat.Tar)
                 && formatByFileName == InArchiveFormat.Tar)
                {
                    detectedFormat = formatByFileName;
                }
                else
                {
                    detectedFormat = formatsByData.Values[0];  // Use the found format with the highest result quality.
                }
            }

            if (detectedFormat == InArchiveFormat.PE) // If the file is an executable, then check if SFX archive or a file with an embedded archive.
            {
                isExecutable = true;
                var data = GetStreamData(stream, 0, SFX_SCAN_LENGTH, true);
                var formatsInPE = new SortedList<int, Tuple<InArchiveFormat, int>>(new DuplicateKeyComparer<int>(true));

                foreach (var format in new[]
                {
                    InArchiveFormat.Zip,
                    InArchiveFormat.SevenZip,
                    InArchiveFormat.Rar4,
                    InArchiveFormat.Rar,
                    InArchiveFormat.Cab,
                    InArchiveFormat.Arj
                })
                {
                    var pos = data.IndexOfSequence(Formats.ArchiveFormatSignatures[format]);

                    if (pos > -1)
                    {
                        int resultQuality = CheckSignatureIsFormat(format, stream, pos);
                        if (resultQuality > 0)
                            formatsInPE.Add(resultQuality, new Tuple<InArchiveFormat, int>(format, pos));
                    }
                }

                if (formatsInPE.Count > 0)
                {
                    detectedFormat = formatsInPE.Values[0].Item1;
                    offset         = formatsInPE.Values[0].Item2;
                }
            }

            if (detectedFormat == (InArchiveFormat)(-1))
            {
                throw new ArgumentException("The stream is invalid or no corresponding signature was found.");
            }

            return detectedFormat;
        }

        /// <summary>
        /// Gets the InArchiveFormat for a specific file name.
        /// </summary>
        /// <param name="fileName">The archive file name.</param>
        /// <param name="offset">The archive beginning offset.</param>
        /// <param name="isExecutable">True if the original format of the file is PE; otherwise, false.</param>
        /// <returns>Corresponding InArchiveFormat.</returns>
        /// <exception cref="System.ArgumentException" />
        public static InArchiveFormat CheckSignature(string fileName, out int offset, out bool isExecutable)
        {
            var exception_FBFN = Formats.FormatByFileName(fileName, out var formatByFileName);

            using (var fs = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                try
                {
                    return CheckSignature(formatByFileName,
                                          fs,
                                          out offset,
                                          out isExecutable);
                }
                catch (ArgumentException exception)
                {
                    offset = 0;
                    isExecutable = false;
                    if (exception_FBFN != null)
                        throw exception_FBFN;

                    return formatByFileName;
                }
            }
        }

        public static int CheckSignatureIsFormat(InArchiveFormat expectedFormat,
                                                 Stream          stream,
                                                 long            checkOffset)
        {
            switch (expectedFormat)
            {
            case InArchiveFormat.Arj:
                return CheckSignatureIsArj(stream, checkOffset);
            case InArchiveFormat.BZip2:
                return CheckSignatureIsBZip2(stream, checkOffset);
            case InArchiveFormat.Cab:
                return CheckSignatureIsCab(stream, checkOffset);
            case InArchiveFormat.Chm:
                return CheckSignatureIsChm(stream, checkOffset);
            case InArchiveFormat.Compound:
                return CheckSignatureIsCompound(stream, checkOffset);
            case InArchiveFormat.Deb:
                return CheckSignatureIsDeb(stream, checkOffset);
            case InArchiveFormat.Dmg:
                return CheckSignatureIsDmg(stream, checkOffset);
            case InArchiveFormat.Elf:
                return CheckSignatureIsElf(stream, checkOffset);
            case InArchiveFormat.Flv:
                return CheckSignatureIsFlv(stream, checkOffset);
            case InArchiveFormat.GZip:
                return CheckSignatureIsGZip(stream, checkOffset);
            case InArchiveFormat.Hfs:
                return CheckSignatureIsHfs(stream, checkOffset);
            case InArchiveFormat.Iso:
                return CheckSignatureIsIso(stream, checkOffset);
            case InArchiveFormat.Lzh:
                return CheckSignatureIsLzh(stream, checkOffset);
            case InArchiveFormat.Lzma:
                return CheckSignatureIsLzma(stream, checkOffset);
            case InArchiveFormat.Lzw:
                return CheckSignatureIsLzw(stream, checkOffset);
            case InArchiveFormat.Mub:
                return CheckSignatureIsMub(stream, checkOffset);
            case InArchiveFormat.PE:
                return CheckSignatureIsPE(stream, checkOffset);
            case InArchiveFormat.Rar:
                return CheckSignatureIsRar(stream, checkOffset);
            case InArchiveFormat.Rar4:
                return CheckSignatureIsRar4(stream, checkOffset);
            case InArchiveFormat.Rpm:
                return CheckSignatureIsRpm(stream, checkOffset);
            case InArchiveFormat.SevenZip:
                return CheckSignatureIsSevenZip(stream, checkOffset);
            case InArchiveFormat.Swf:
                return CheckSignatureIsSwf(stream, checkOffset);
            case InArchiveFormat.Tar:
                return CheckSignatureIsTar(stream, checkOffset);
            case InArchiveFormat.Udf:
                return CheckSignatureIsUdf(stream, checkOffset);
            case InArchiveFormat.Vhd:
                return CheckSignatureIsVhd(stream, checkOffset);
            case InArchiveFormat.Wim:
                return CheckSignatureIsWim(stream, checkOffset);
            case InArchiveFormat.Xar:
                return CheckSignatureIsXar(stream, checkOffset);
            case InArchiveFormat.XZ:
                return CheckSignatureIsXZ(stream, checkOffset);
            case InArchiveFormat.Zip:
                return CheckSignatureIsZip(stream, checkOffset);

            default:
                throw new NotSupportedException();
            }
        }

        private static int CheckSignatureIsArj(Stream stream, long checkOffset)
        {
            int resultQuality = 0;
            var expectedSignature = Formats.Signature_ArjArchive;
            long position = checkOffset;

            byte[] actualSignature = GetStreamData(stream, position, expectedSignature.Count);
            if (expectedSignature.SequenceEqual(actualSignature))
                resultQuality += expectedSignature.Count;

            return resultQuality;
        }

        private static int CheckSignatureIsBZip2(Stream stream, long checkOffset)
        {
            int resultQuality = 0;
            var expectedSignature = Formats.Signature_BZip2Archive;
            long position = checkOffset;

            byte[] actualSignature = GetStreamData(stream, position, expectedSignature.Count);
            if (expectedSignature.SequenceEqual(actualSignature))
                resultQuality += expectedSignature.Count;

            return resultQuality;
        }

        private static int CheckSignatureIsCab(Stream stream, long checkOffset)
        {
            int resultQuality = 0;
            var expectedSignature = Formats.Signature_CabArchive;
            long position = checkOffset;

            byte[] actualSignature = GetStreamData(stream, position, expectedSignature.Count);
            if (expectedSignature.SequenceEqual(actualSignature))
                resultQuality += expectedSignature.Count;

            return resultQuality;
        }

        private static int CheckSignatureIsChm(Stream stream, long checkOffset)
        {
            int resultQuality = 0;
            var expectedSignature = Formats.Signature_Chm;
            long position = checkOffset;

            byte[] actualSignature = GetStreamData(stream, position, expectedSignature.Count);
            if (expectedSignature.SequenceEqual(actualSignature))
                resultQuality += expectedSignature.Count;

            return resultQuality;
        }

        private static int CheckSignatureIsCompound(Stream stream, long checkOffset)
        {
            int resultQuality = 0;
            var expectedSignature = Formats.Signature_CompoundFile;
            long position = checkOffset;

            byte[] actualSignature = GetStreamData(stream, position, expectedSignature.Count);
            if (expectedSignature.SequenceEqual(actualSignature))
                resultQuality += expectedSignature.Count;

            return resultQuality;
        }

        private static int CheckSignatureIsDeb(Stream stream, long checkOffset)
        {
            int resultQuality = 0;
            var expectedSignature = Formats.Signature_DEB;
            long position = checkOffset;

            byte[] actualSignature = GetStreamData(stream, position, expectedSignature.Count);
            if (expectedSignature.SequenceEqual(actualSignature))
                resultQuality += expectedSignature.Count;

            return resultQuality;
        }

        private static int CheckSignatureIsDmg(Stream stream, long checkOffset)
        {
            int resultQuality = 0;
            var expectedSignature = Formats.Signature_DMG_AppleDiskImage;
            long position = checkOffset;

            long signaturePosition = stream.Length - 512;
            byte[] actualSignature = GetStreamData(stream, signaturePosition, expectedSignature.Count);
            if (expectedSignature.SequenceEqual(actualSignature))
                resultQuality += expectedSignature.Count;

            return resultQuality;
        }

        private static int CheckSignatureIsElf(Stream stream, long checkOffset)
        {
            int resultQuality = 0;
            var expectedSignature = Formats.Signature_Elf;
            long position = checkOffset;

            byte[] actualSignature = GetStreamData(stream, position, expectedSignature.Count);
            if (expectedSignature.SequenceEqual(actualSignature))
                resultQuality += expectedSignature.Count;

            return resultQuality;
        }

        private static int CheckSignatureIsFlv(Stream stream, long checkOffset)
        {
            int resultQuality = 0;
            var expectedSignature = Formats.Signature_Flv;
            long position = checkOffset;

            byte[] actualSignature = GetStreamData(stream, position, expectedSignature.Count);
            if (expectedSignature.SequenceEqual(actualSignature))
                resultQuality += expectedSignature.Count;

            return resultQuality;
        }

        private static int CheckSignatureIsGZip(Stream stream, long checkOffset)
        {
            int resultQuality = 0;
            var expectedSignature = Formats.Signature_GZipArchive;
            long position = checkOffset;

            byte[] actualSignature = GetStreamData(stream, position, expectedSignature.Count);
            if (expectedSignature.SequenceEqual(actualSignature))
                resultQuality += expectedSignature.Count;

            return resultQuality;
        }

        private static int CheckSignatureIsHfs(Stream stream, long checkOffset)
        {
            int resultQuality = 0;
            var expectedSignature = Formats.Signature_Hfs;
            long position = 0x400 + checkOffset;

            byte[] actualSignature = GetStreamData(stream, position, expectedSignature.Count);
            if (expectedSignature.SequenceEqual(actualSignature))
                resultQuality += expectedSignature.Count;

            return resultQuality;
        }

        private static int CheckSignatureIsIso(Stream stream, long checkOffset)
        {
            int resultQuality = 0;
            var expectedSignature = Formats.Signature_Iso;
            long position1 = 0x8001 + checkOffset;
            long position2 = 0x8801 + checkOffset;
            long position3 = 0x9001 + checkOffset;

            byte[] actualSignature1 = GetStreamData(stream, position1, expectedSignature.Count);
            byte[] actualSignature2 = GetStreamData(stream, position2, expectedSignature.Count);
            byte[] actualSignature3 = GetStreamData(stream, position3, expectedSignature.Count);
            if (expectedSignature.SequenceEqual(actualSignature1)
             || expectedSignature.SequenceEqual(actualSignature2)
             || expectedSignature.SequenceEqual(actualSignature3))
                resultQuality += expectedSignature.Count;

            return resultQuality;
        }

        private static int CheckSignatureIsLzh(Stream stream, long checkOffset)
        {
            int resultQuality = 0;
            var expectedSignature = Formats.Signature_LzhArchive;
            long position = 2 + checkOffset;

            byte[] actualSignature = GetStreamData(stream, position, expectedSignature.Count);
            if (expectedSignature.SequenceEqual(actualSignature))
                resultQuality += expectedSignature.Count;

            return resultQuality;
        }

        private static int CheckSignatureIsLzma(Stream stream, long checkOffset)
        {
            int resultQuality = 0;
            var expectedSignature = Formats.Signature_LzmaArchive;
            long position = checkOffset;

            byte[] actualSignature = GetStreamData(stream, position, expectedSignature.Count);
            if (expectedSignature.SequenceEqual(actualSignature))
                resultQuality += expectedSignature.Count;

            return resultQuality;
        }

        private static int CheckSignatureIsLzw(Stream stream, long checkOffset)
        {
            int resultQuality = 0;
            var expectedSignature = Formats.Signature_LzwArchive;
            long position = checkOffset;

            byte[] actualSignature = GetStreamData(stream, position, expectedSignature.Count);
            if (expectedSignature.SequenceEqual(actualSignature))
                resultQuality += expectedSignature.Count;

            return resultQuality;
        }

        private static int CheckSignatureIsMub(Stream stream, long checkOffset)
        {
            int resultQuality = 0;
            var expectedSignature = Formats.Signature_Mub;
            long position = checkOffset;

            byte[] actualSignature = GetStreamData(stream, position, expectedSignature.Count);
            if (expectedSignature.SequenceEqual(actualSignature))
                resultQuality += expectedSignature.Count;

            return resultQuality;
        }

        /// <summary>
        /// Check whether the stream contains the signature of the PE file format and return the quality of the result of the
        /// check.
        /// </summary>
        /// <param name="stream">The stream that is checked.</param>
        /// <returns>A numeric value that indicates the quality of the result of the check. If 0, the check has failed.</returns>
        /// <remarks>
        /// http://docs.microsoft.com/en-us/windows/win32/debug/pe-format <br />
        /// http://de.wikipedia.org/wiki/Portable_Executable
        /// </remarks>
        private static int CheckSignatureIsPE(Stream stream, long checkOffset)
        {
            if (stream.Length < 0x40 + checkOffset)
                return 0;

            int resultQuality = 0;

            var expectedSignature_MZ = Formats.Signature_DosExecutable;
            var expectedSignature_PE = Formats.Signature_PE_PortableExecutable;
            long position_MZ = checkOffset;
            long position_PEPosition = 0x3c + checkOffset;  // The position of the PE header is defined in 4 bytes at offset 0x3c from the file header begin.

            byte[] actualSignature_MZ = GetStreamData(stream, position_MZ, expectedSignature_MZ.Count);
            if (expectedSignature_MZ.SequenceEqual(actualSignature_MZ))
            {
                int peHeaderPosition = BitConverter.ToInt32(GetStreamData(stream, position_PEPosition, 4), 0);
                long position_PE = peHeaderPosition + checkOffset;
                byte[] actualSignature_PE = GetStreamData(stream, position_PE, expectedSignature_PE.Count);

                if (expectedSignature_PE.SequenceEqual(actualSignature_PE))
                {
                    resultQuality += expectedSignature_MZ.Count
                                   + expectedSignature_PE.Count;
                }
            }

            return resultQuality;
        }

        private static int CheckSignatureIsRar(Stream stream, long checkOffset)
        {
            int resultQuality = 0;
            var expectedSignature = Formats.Signature_RarArchive;
            long position = checkOffset;

            byte[] actualSignature = GetStreamData(stream, position, expectedSignature.Count);
            if (expectedSignature.SequenceEqual(actualSignature))
                resultQuality += expectedSignature.Count;

            return resultQuality;
        }

        private static int CheckSignatureIsRar4(Stream stream, long checkOffset)
        {
            int resultQuality = 0;
            var expectedSignature = Formats.Signature_Rar4Archive;
            long position = checkOffset;

            byte[] actualSignature = GetStreamData(stream, position, expectedSignature.Count);
            if (expectedSignature.SequenceEqual(actualSignature))
                resultQuality += expectedSignature.Count;

            return resultQuality;
        }

        private static int CheckSignatureIsRpm(Stream stream, long checkOffset)
        {
            int resultQuality = 0;
            var expectedSignature = Formats.Signature_Rpm;
            long position = checkOffset;

            byte[] actualSignature = GetStreamData(stream, position, expectedSignature.Count);
            if (expectedSignature.SequenceEqual(actualSignature))
                resultQuality += expectedSignature.Count;

            return resultQuality;
        }

        private static int CheckSignatureIsSevenZip(Stream stream, long checkOffset)
        {
            int resultQuality = 0;
            var expectedSignature = Formats.Signature_SevenZipArchive;
            long position = checkOffset;

            byte[] actualSignature = GetStreamData(stream, position, expectedSignature.Count);
            if (expectedSignature.SequenceEqual(actualSignature))
                resultQuality += expectedSignature.Count;

            return resultQuality;
        }

        private static int CheckSignatureIsSwf(Stream stream, long checkOffset)
        {
            int resultQuality = 0;
            var expectedSignature = Formats.Signature_Swf;
            long position = checkOffset;

            byte[] actualSignature = GetStreamData(stream, position, expectedSignature.Count);
            if (expectedSignature.SequenceEqual(actualSignature))
                resultQuality += expectedSignature.Count;

            return resultQuality;
        }

        private static int CheckSignatureIsTar(Stream stream, long checkOffset)
        {
            const int headerSize = 512;
            const int checksumPosInHeader = 148;
            const int checksumSize = 8;

            if (stream.Length < headerSize)
                return 0;

            int resultQuality = 0;
            var expectedSignature = Formats.Signature_USTarArchive;
            long position = checkOffset + 257;

            // Tar formats "ustar" and "pax" contain the text "ustar" at position 257.
            byte[] actualSignature = GetStreamData(stream, position, expectedSignature.Count);
            if (expectedSignature.SequenceEqual(actualSignature))
                resultQuality += expectedSignature.Count;

            // Tar format "v7" does not contain any signature. Therefore always check the checksum of the header, which is available in all Tar formats.
            position = checkOffset;
            stream.Position = position;
            int actualChecksum;
            var headerWithoutChecksum = new List<byte>();
            using (var reader = new BinaryReader(stream, Encoding.Default, true))
            {
                headerWithoutChecksum.AddRange(reader.ReadBytes(checksumPosInHeader));
                headerWithoutChecksum.AddRange(Encoding.ASCII.GetBytes(new string(' ', checksumSize)));
                var actualChecksumBytes = reader.ReadBytes(checksumSize).SkipWhile(value => value < 0x30).TakeWhile(value => value >= 0x30); // the checksum is an ASCII text of the octal value, preceded and/or terminated with space (0x20) or 0x00.
                actualChecksum = actualChecksumBytes.Reverse().Select((value, index) => (value - 0x30) * (int)Math.Pow(8, index)).Sum(); // convert each byte to a number (by "- 0x30"), then multiply with octal base at each index, then sum all values.
                headerWithoutChecksum.AddRange(reader.ReadBytes(headerSize - checksumPosInHeader - checksumSize));
            }

            int expectedChecksum = headerWithoutChecksum.Select(value => (int)value).Sum();
            if (actualChecksum == expectedChecksum)
                resultQuality += headerSize / 10; // Arbitrary decision: use 10% of header size because only the checksum was checked.

            return resultQuality;
        }

        private static int CheckSignatureIsUdf(Stream stream, long checkOffset)
        {
            int resultQuality = 0;
            var expectedSignature = Formats.Signature_Udf;
            long position = checkOffset;

            byte[] actualSignature = GetStreamData(stream, position, expectedSignature.Count);
            if (expectedSignature.SequenceEqual(actualSignature))
                resultQuality += expectedSignature.Count;

            return resultQuality;
        }

        private static int CheckSignatureIsVhd(Stream stream, long checkOffset)
        {
            int resultQuality = 0;
            var expectedSignature = Formats.Signature_Vhd;
            long position = checkOffset;

            byte[] actualSignature = GetStreamData(stream, position, expectedSignature.Count);
            if (expectedSignature.SequenceEqual(actualSignature))
                resultQuality += expectedSignature.Count;

            return resultQuality;
        }

        private static int CheckSignatureIsWim(Stream stream, long checkOffset)
        {
            int resultQuality = 0;
            var expectedSignature = Formats.Signature_WimArchive;
            long position = checkOffset;

            byte[] actualSignature = GetStreamData(stream, position, expectedSignature.Count);
            if (expectedSignature.SequenceEqual(actualSignature))
                resultQuality += expectedSignature.Count;

            return resultQuality;
        }

        private static int CheckSignatureIsXar(Stream stream, long checkOffset)
        {
            int resultQuality = 0;
            var expectedSignature = Formats.Signature_XarArchive;
            long position = checkOffset;

            byte[] actualSignature = GetStreamData(stream, position, expectedSignature.Count);
            if (expectedSignature.SequenceEqual(actualSignature))
                resultQuality += expectedSignature.Count;

            return resultQuality;
        }

        private static int CheckSignatureIsXZ(Stream stream, long checkOffset)
        {
            int resultQuality = 0;
            var expectedSignature = Formats.Signature_XZArchive;
            long position = checkOffset;

            byte[] actualSignature = GetStreamData(stream, position, expectedSignature.Count);
            if (expectedSignature.SequenceEqual(actualSignature))
                resultQuality += expectedSignature.Count;

            return resultQuality;
        }

        private static int CheckSignatureIsZip(Stream stream, long checkOffset)
        {
            int resultQuality = 0;
            var expectedSignature = Formats.Signature_ZipArchive;
            long position = checkOffset;

            byte[] actualSignature = GetStreamData(stream, position, expectedSignature.Count);
            if (expectedSignature.SequenceEqual(actualSignature))
                resultQuality += expectedSignature.Count;

            return resultQuality;
        }

        private static byte[] GetStreamData(Stream stream,
                                            long   position,
                                            int    length,
                                            bool   acceptShorterData = false)
        {
            if (length < 1)
                throw new ArgumentOutOfRangeException(nameof(length), length, "value must be >= 1.");

            if (position < 0
             || position > stream.Length) // Start of read would be before start of stream or after end of stream.
                return new byte[0];

            if (position + length > stream.Length) // End of read would be after end of stream.
            {
                if (!acceptShorterData)
                    return new byte[0];

                length = (int)(stream.Length - position);
            }

            var data = new byte[length];
            var bytesRequired = length;
            var index = 0;
            stream.Position = position;

            while (bytesRequired > 0)
            {
                var bytesRead = stream.Read(data, index, bytesRequired);
                bytesRequired -= bytesRead;
                index += bytesRead;
            }

            return data;
        }
    }
#endif
}