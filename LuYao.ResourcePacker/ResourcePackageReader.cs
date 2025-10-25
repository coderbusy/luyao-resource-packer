using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace LuYao.ResourcePacker
{
    /// <summary>
    /// Provides functionality to read resources from a packaged resource file.
    /// </summary>
    public class ResourcePackageReader : IDisposable
    {
        private readonly FileStream _fileStream;
        private readonly Dictionary<string, ResourceEntry> _resourceIndex;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="ResourcePackageReader"/> class.
        /// </summary>
        /// <param name="filePath">The path to the resource package file.</param>
        public ResourcePackageReader(string filePath)
        {
            _fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            _resourceIndex = new Dictionary<string, ResourceEntry>();
            LoadIndex();
        }

        private void LoadIndex()
        {
            // TODO: Implement index loading from file header
        }

        /// <summary>
        /// Gets a list of all available resource keys in the package.
        /// </summary>
        public IEnumerable<string> ResourceKeys => _resourceIndex.Keys;

        /// <summary>
        /// Reads a resource as a byte array asynchronously.
        /// </summary>
        /// <param name="resourceKey">The key of the resource to read.</param>
        /// <returns>A task that represents the asynchronous read operation.</returns>
        public async Task<byte[]> ReadResourceAsync(string resourceKey)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(ResourcePackageReader));

            if (!_resourceIndex.TryGetValue(resourceKey, out var entry))
                throw new KeyNotFoundException($"Resource with key '{resourceKey}' not found.");

            var buffer = new byte[entry.Length];
            _fileStream.Seek(entry.Offset, SeekOrigin.Begin);
            await _fileStream.ReadAsync(buffer, 0, buffer.Length);
            return buffer;
        }

        /// <summary>
        /// Reads a resource as a string asynchronously.
        /// </summary>
        /// <param name="resourceKey">The key of the resource to read.</param>
        /// <returns>A task that represents the asynchronous read operation.</returns>
        public async Task<string> ReadResourceAsStringAsync(string resourceKey)
        {
            var bytes = await ReadResourceAsync(resourceKey);
            return System.Text.Encoding.UTF8.GetString(bytes);
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _fileStream?.Dispose();
                _disposed = true;
            }
        }
    }

    internal class ResourceEntry
    {
        public long Offset { get; set; }
        public int Length { get; set; }
    }
}