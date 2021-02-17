using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ICSharpCode.SharpZipLib.Zip;
using ICSharpCode.SharpZipLib.Zip.Compression;
using Curiosity.TempFiles;
using Curiosity.Tools;

namespace Curiosity.Archiver.SharpZip
{
    /// <summary>
    /// SharpZib archiver.
    /// </summary>
    public class SharpZipArchiver : IArchiver
    {
        /// <summary>
        /// Factory to create <see cref="TempFileStream"/>.
        /// </summary>
        private readonly ITempFileStreamFactory _tempFileStreamFactory;
        
        /// <summary>
        /// Size of buffer for reading data from file.
        /// </summary>
        /// <remarks>
        /// About better buffer size: https://www.javamex.com/tutorials/io/input_stream_buffer_size.shtml
        /// </remarks>
        private const int BufferSize = 32768;

        /// <summary>
        /// Level of ZIP compression.
        /// </summary>
        /// <remarks>
        /// 0-9, 9 being the highest level of compression
        /// </remarks>
        private const int ZipLevel = 9;

        private readonly ArrayPool<byte> _arrayPool;

        private readonly TempFileOptions _tempFileOptions;
        
        public SharpZipArchiver(
            ITempFileStreamFactory tempFileStreamFactory, 
            TempFileOptions tempFileOptions)
        {
            _tempFileStreamFactory = tempFileStreamFactory;
            _tempFileOptions = tempFileOptions;
            _arrayPool = ArrayPool<byte>.Create();
        }

        /// <inheritdoc />
        public Task<TempFileStream> ZipDirAsync(
            string dirToCompressPath,
            bool useZip64 = true, 
            CancellationToken cts = default)
        {                
            if (String.IsNullOrWhiteSpace(dirToCompressPath))
                throw new ArgumentNullException(nameof(dirToCompressPath));
            
            if (!Directory.Exists(dirToCompressPath))
                throw new ArgumentException($"Directory does not exist: \"{dirToCompressPath}\"'");

            var zipFileName = $"{Guid.NewGuid()}.zip";
            var archivePath = Path.Combine(_tempFileOptions.TempPath, zipFileName);
            try
            {
                var fastZip = new FastZip
                {
                    UseZip64 = useZip64 ? UseZip64.On : UseZip64.Off,
                    RestoreDateTimeOnExtract = true,
                    RestoreAttributesOnExtract = true,
                    CompressionLevel = Deflater.CompressionLevel.BEST_COMPRESSION
                };
                
                fastZip.CreateZip(archivePath, dirToCompressPath, true, (string)null!, null);
                return Task.FromResult(new TempFileStream(archivePath, FileMode.Open));
            }
            catch
            {
                if (File.Exists(archivePath))
                {
                    File.Delete(archivePath);
                }
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<TempFileStream> ZipFilesToStreamAsync(
            IList<string> sourceFiles,
            bool useZip64 = true,
            string? zipFileName = null,
            IList<string>? zipFileNames = null, 
            CancellationToken cts = default)
        {
            if (String.IsNullOrWhiteSpace(zipFileName))
                throw new ArgumentNullException(nameof(zipFileName));
            if (sourceFiles == null) throw new ArgumentNullException(nameof(sourceFiles));
            if (zipFileNames != null && zipFileNames.Count != sourceFiles.Count)
                throw new ArgumentException($"Items count in {sourceFiles} and {zipFileNames} must be equal");
            
            
            var tempStream = _tempFileStreamFactory.CreateTempFileStream(GetZipFileName(zipFileName));
            
            var buffer = _arrayPool.Rent(BufferSize);
            try
            {
                using (var zipStream = new ZipOutputStream(tempStream))
                {
                    zipStream.IsStreamOwner = false;
                    await ZipFilesAsync(buffer, zipStream, sourceFiles, useZip64, zipFileNames, cts);
                }

                // we need to set position to start because temp stream can be used in another places
                tempStream.Position = 0;
                return tempStream;
            }
            catch
            {
                _arrayPool.Return(buffer);
                tempStream?.Dispose();
                throw;
            }
        }

        private string GetZipFileName(string? zipFileName = null)
        {
            return String.IsNullOrWhiteSpace(zipFileName)
                ? $"{Guid.NewGuid()}.zip"
                : zipFileName;
        }
        
        private async Task ZipFilesAsync(
            byte[] buffer,
            [NotNull] ZipOutputStream zipStream,
            [NotNull] IList<string> sourceFiles,
            bool useZip64 = true,
            IList<string>? zipFileNames = null,
            CancellationToken cts = default)
        {
            zipStream.SetLevel(ZipLevel);
            zipStream.UseZip64 = useZip64 ? UseZip64.On : UseZip64.Off;

            for (var i = 0; i < sourceFiles.Count; i++)
            {
                var sourceFileName = sourceFiles[i];
                var destFileName = zipFileNames != null 
                    ? zipFileNames[i] 
                    : new FileInfo(sourceFileName).Name;

                var fileInfo = new FileInfo(sourceFileName);
                var entry = new ZipEntry(ZipEntry.CleanName(destFileName))
                {
                    DateTime = fileInfo.LastWriteTime, // Note the zip format stores 2 second granularity
                    Size = fileInfo.Length,
                    IsUnicodeText = true
                };
                zipStream.PutNextEntry(entry);

                using (var streamReader = File.OpenRead(sourceFileName))
                {
                    await streamReader.CopyToAsync(zipStream, buffer, cts);
                }
            }
            zipStream.Finish();
            zipStream.Close();
        }

        /// <inheritdoc />
        public async Task<string> ZipFilesToFileAsync(
            IList<string> sourceFiles,
            bool useZip64 = true,
            string? zipFileName = null,
            IList<string>? zipFileNames = null, 
            CancellationToken cts = default)
        {
            if (sourceFiles == null) throw new ArgumentNullException(nameof(sourceFiles));
            if (zipFileNames != null && zipFileNames.Count != sourceFiles.Count)
                throw new ArgumentException($"Items count in {sourceFiles} and {zipFileNames} must be equal");
            
            
            var zipFilePath = Path.Combine(_tempFileOptions.TempPath, GetZipFileName(zipFileName));
            
            var buffer = _arrayPool.Rent(BufferSize);
            try
            {
                using (var zipStream = new ZipOutputStream(File.Create(zipFilePath)))
                {
                    await ZipFilesAsync(buffer, zipStream, sourceFiles, useZip64, zipFileNames, cts);
                }

                return zipFilePath;
            }
            catch
            {
                if (File.Exists(zipFilePath))
                {
                    File.Delete(zipFilePath);
                }
                
                _arrayPool.Return(buffer);
                throw;
            }
        }
    }
}