using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace LuYao.ResourcePacker
{
    public class ResourcePacker
    {
        /// <summary>
        /// The current version of the resource package format.
        /// </summary>
        public const byte FormatVersion = 1;

        private readonly string _sourceDirectory;

        // Compression thresholds
        private const int MinCompressionSize = 255;
        private const int FullCompressionThreshold = 4 * 1024; // 4KB
        private const int SampleSize = 8 * 1024; // 8KB
        private const double MinCompressionRatio = 0.05; // 5% minimum compression

        // Known compressed file extensions that should skip compression
        private static readonly HashSet<string> CompressedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp", ".ico", // Images
            ".zip", ".gz", ".7z", ".rar", ".tar", ".bz2", // Archives
            ".mp3", ".mp4", ".avi", ".mkv", ".flv", ".mov", // Media
            ".pdf", // Documents
            ".woff", ".woff2", ".ttf", ".otf" // Fonts
        };

        public ResourcePacker(string sourceDirectory)
        {
            if (string.IsNullOrEmpty(sourceDirectory))
                throw new ArgumentNullException(nameof(sourceDirectory));

            _sourceDirectory = sourceDirectory;
        }

        public void PackResources(string outputFilePath)
        {
            var resources = CollectResources();
            WriteResourcePackage(outputFilePath, resources);
        }

        private IEnumerable<ResourceFile> CollectResources()
        {
            if (!Directory.Exists(_sourceDirectory))
            {
                return Enumerable.Empty<ResourceFile>();
            }

            return Directory.GetFiles(_sourceDirectory, "*", SearchOption.AllDirectories)
                .Select(file => 
                {
                    var content = File.ReadAllBytes(file);
                    var (isCompressed, compressedContent) = TryCompress(file, content);
                    
                    return new ResourceFile
                    {
                        FullPath = file,
                        Key = ResourceKeyHelper.GetResourceKey(file),
                        OriginalContent = content,
                        Content = isCompressed ? compressedContent : content,
                        IsCompressed = isCompressed
                    };
                });
        }

        private void WriteResourcePackage(string outputFilePath, IEnumerable<ResourceFile> resources)
        {
            var resourceList = resources.OrderBy(r => r.Key).ToList();
            
            using var fs = new FileStream(outputFilePath, FileMode.Create);
            using var writer = new BinaryWriter(fs);

            // Write version number (requirement 1)
            writer.Write(FormatVersion);

            // Write resource count
            writer.Write(resourceList.Count);

            // Write index (key, original length, compressed length, isCompressed flag)
            foreach (var resource in resourceList)
            {
                writer.Write(resource.Key);
                writer.Write(resource.OriginalContent.Length); // Original size
                writer.Write(resource.Content.Length); // Stored size (compressed or original)
                writer.Write(resource.IsCompressed); // Compression flag
            }

            // Write content
            foreach (var resource in resourceList)
            {
                writer.Write(resource.Content);
            }
        }

        /// <summary>
        /// Attempts to compress the file content based on tiered compression rules.
        /// </summary>
        /// <param name="filePath">The path to the file being compressed.</param>
        /// <param name="content">The file content to compress.</param>
        /// <returns>A tuple indicating if compression was applied and the resulting content.</returns>
        private (bool isCompressed, byte[] content) TryCompress(string filePath, byte[] content)
        {
            // Rule 1: Files smaller than 255 bytes are not compressed
            if (content.Length < MinCompressionSize)
            {
                return (false, content);
            }

            // Skip already compressed file formats
            var extension = Path.GetExtension(filePath);
            if (CompressedExtensions.Contains(extension))
            {
                return (false, content);
            }

            // Rule 2: Files >= 255 bytes and < 4KB - compress entire file
            if (content.Length < FullCompressionThreshold)
            {
                return EvaluateCompression(content, content);
            }

            // Rule 3: Files >= 4KB - sample first 8KB for evaluation
            var sampleData = new byte[Math.Min(SampleSize, content.Length)];
            Array.Copy(content, 0, sampleData, 0, sampleData.Length);
            
            var (shouldCompress, _) = EvaluateCompression(sampleData, sampleData);
            
            if (shouldCompress)
            {
                // Sample indicates good compression, compress the full file
                return EvaluateCompression(content, content);
            }

            return (false, content);
        }

        /// <summary>
        /// Evaluates whether compression meets the minimum compression ratio threshold.
        /// </summary>
        /// <param name="sampleData">Data to evaluate for compression ratio.</param>
        /// <param name="actualData">Actual data to compress if evaluation passes.</param>
        /// <returns>A tuple indicating if compression was beneficial and the compressed data.</returns>
        private (bool isCompressed, byte[] compressedData) EvaluateCompression(byte[] sampleData, byte[] actualData)
        {
            var sampleCompressed = CompressData(sampleData);
            var compressionRatio = 1.0 - ((double)sampleCompressed.Length / sampleData.Length);

            // Only compress if we achieve at least 5% compression
            if (compressionRatio >= MinCompressionRatio)
            {
                // If sample and actual are the same size, we sampled the entire file
                if (sampleData.Length == actualData.Length)
                {
                    return (true, sampleCompressed);
                }
                // Otherwise compress the actual data
                return (true, CompressData(actualData));
            }

            return (false, actualData);
        }

        /// <summary>
        /// Compresses data using GZip compression.
        /// </summary>
        /// <param name="data">The data to compress.</param>
        /// <returns>The compressed data.</returns>
        private byte[] CompressData(byte[] data)
        {
            using var outputStream = new MemoryStream();
            using (var gzipStream = new GZipStream(outputStream, CompressionMode.Compress))
            {
                gzipStream.Write(data, 0, data.Length);
            }
            return outputStream.ToArray();
        }



        private class ResourceFile
        {
            public string FullPath { get; set; }
            public string Key { get; set; }
            public byte[] OriginalContent { get; set; }
            public byte[] Content { get; set; }
            public bool IsCompressed { get; set; }
        }
    }
}