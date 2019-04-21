namespace SevenZip
{
    using System;
    using System.IO;

    partial class SevenZipExtractor
    {
        #region Asynchronous core methods

        /// <summary>
        /// Recreates the instance of the SevenZipExtractor class.
        /// Used in asynchronous methods.
        /// </summary>
        private void RecreateInstanceIfNeeded()
        {
            if (NeedsToBeRecreated)
            {
                NeedsToBeRecreated = false;
                Stream backupStream = null;
                string backupFileName = null;
                if (String.IsNullOrEmpty(_fileName))
                {
                    backupStream = _inStream;
                }
                else
                {
                    backupFileName = _fileName;
                }
                CommonDispose();
                if (backupStream == null)
                {
                    Init(backupFileName);
                }
                else
                {
                    Init(backupStream);
                }
            }
        }

        internal override void SaveContext()
        {
            DisposedCheck();
            _asynchronousDisposeLock = true;
            base.SaveContext();
        }

        internal override void ReleaseContext()
        {
            base.ReleaseContext();
            _asynchronousDisposeLock = false;
        }

        #endregion

        #region Delegates
        /// <summary>
        /// The delegate to use in BeginExtractArchive.
        /// </summary>
        /// <param name="directory">The directory where the files are to be unpacked.</param>
        private delegate void ExtractArchiveDelegate(string directory);

        /// <summary>
        /// The delegate to use in BeginExtractFile (by file name).
        /// </summary>
        /// <param name="fileName">The file full name in the archive file table.</param>
        /// <param name="stream">The stream where the file is to be unpacked.</param>
        private delegate void ExtractFileByFileNameDelegate(string fileName, Stream stream);

        /// <summary>
        /// The delegate to use in BeginExtractFile (by index).
        /// </summary>
        /// <param name="index">Index in the archive file table.</param>
        /// <param name="stream">The stream where the file is to be unpacked.</param>
        private delegate void ExtractFileByIndexDelegate(int index, Stream stream);

        /// <summary>
        /// The delegate to use in BeginExtractFiles(string directory, params int[] indexes).
        /// </summary>
        /// <param name="indexes">indexes of the files in the archive file table.</param>
        /// <param name="directory">Directory where the files are to be unpacked.</param>
        private delegate void ExtractFiles1Delegate(string directory, int[] indexes);

        /// <summary>
        /// The delegate to use in BeginExtractFiles(string directory, params string[] fileNames).
        /// </summary>
        /// <param name="fileNames">Full file names in the archive file table.</param>
        /// <param name="directory">Directory where the files are to be unpacked.</param>
        private delegate void ExtractFiles2Delegate(string directory, string[] fileNames);

        /// <summary>
        /// The delegate to use in BeginExtractFiles(ExtractFileCallback extractFileCallback).
        /// </summary>
        /// <param name="extractFileCallback">The callback to call for each file in the archive.</param>
        private delegate void ExtractFiles3Delegate(ExtractFileCallback extractFileCallback);
        private delegate void ExtractFiles4Delegate(ExtractFileCallback extractFileCallback, Func<uint, OutStreamWrapper> getStream, Action<FileInfoEventArgs> FileExtractStart, Action<FileInfoEventArgs> FileExtractComplete, Func<AskMode, AskMode> prepareCheck, int? entryIndex);
        #endregion

        /// <summary>
        /// Unpacks the whole archive asynchronously to the specified directory name at the specified priority.
        /// </summary>
        /// <param name="directory">The directory where the files are to be unpacked.</param>
        public void BeginExtractArchive(string directory)
        {
            SaveContext();
            new ExtractArchiveDelegate(ExtractArchive).BeginInvoke(directory, AsyncCallbackImplementation, this);
        }

        /// <summary>
        /// Unpacks the file asynchronously by its name to the specified stream.
        /// </summary>
        /// <param name="fileName">The file full name in the archive file table.</param>
        /// <param name="stream">The stream where the file is to be unpacked.</param>
        public void BeginExtractFile(string fileName, Stream stream)
        {
            SaveContext();
            new ExtractFileByFileNameDelegate(ExtractFile).BeginInvoke(fileName, stream, AsyncCallbackImplementation, this);
        }

        /// <summary>
        /// Unpacks the file asynchronously by its index to the specified stream.
        /// </summary>
        /// <param name="index">Index in the archive file table.</param>
        /// <param name="stream">The stream where the file is to be unpacked.</param>
        public void BeginExtractFile(int index, Stream stream)
        {
            SaveContext();
            new ExtractFileByIndexDelegate(ExtractFile).BeginInvoke(index, stream, AsyncCallbackImplementation, this);
        }

        /// <summary>
        /// Unpacks files asynchronously by their indices to the specified directory.
        /// </summary>
        /// <param name="indexes">indexes of the files in the archive file table.</param>
        /// <param name="directory">Directory where the files are to be unpacked.</param>
        public void BeginExtractFiles(string directory, params int[] indexes)
        {
            SaveContext();
            new ExtractFiles1Delegate(ExtractFiles).BeginInvoke(directory, indexes, AsyncCallbackImplementation, this);
        }

        /// <summary>
        /// Unpacks files asynchronously by their full names to the specified directory.
        /// </summary>
        /// <param name="fileNames">Full file names in the archive file table.</param>
        /// <param name="directory">Directory where the files are to be unpacked.</param>
        public void BeginExtractFiles(string directory, params string[] fileNames)
        {
            SaveContext();
            new ExtractFiles2Delegate(ExtractFiles).BeginInvoke(directory, fileNames, AsyncCallbackImplementation, this);
        }

        /// <summary>
        /// Extracts files from the archive asynchronously, giving a callback the choice what
        /// to do with each file. The order of the files is given by the archive.
        /// 7-Zip (and any other solid) archives are NOT supported.
        /// </summary>
        /// <param name="extractFileCallback">The callback to call for each file in the archive.</param>
        public void BeginExtractFiles(ExtractFileCallback extractFileCallback, Func<uint, OutStreamWrapper> getStream = null, Action<FileInfoEventArgs> FileExtractStart = null, Action<FileInfoEventArgs> FileExtractComplete = null, Func<AskMode, AskMode> prepareCheck = null, int? entryIndex = null)
        {
            SaveContext();
            new ExtractFiles4Delegate(ExtractFiles).BeginInvoke(extractFileCallback, getStream, FileExtractStart, FileExtractComplete, prepareCheck, entryIndex, AsyncCallbackImplementation, this);
        }
    }
}
