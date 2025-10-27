using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading.Tasks;

namespace LuYao.ResourcePacker
{
    /// <summary>
    /// Provides functionality to read resources from a packaged resource file.
    /// This class is thread-safe for concurrent read operations by creating independent FileStream instances per operation.
    /// </summary>
    public class ResourcePackageReader
    {
        private readonly string _filePath;
        private readonly Dictionary<string, ResourceEntry> _resourceIndex;

        /// <summary>
        /// Initializes a new instance of the <see cref="ResourcePackageReader"/> class.
        /// </summary>
        /// <param name="filePath">The path to the resource package file.</param>
        public ResourcePackageReader(string filePath)
        {
            _filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
            _resourceIndex = new Dictionary<string, ResourceEntry>();
            LoadIndex();
        }

        private void LoadIndex()
        {
            using var fileStream = new FileStream(_filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var reader = new BinaryReader(fileStream, System.Text.Encoding.UTF8, leaveOpen: false);
            
            // Read version number
            var version = reader.ReadByte();
            if (version != ResourcePacker.FormatVersion)
            {
                throw new InvalidDataException($"Unsupported file version: {version}. Expected version {ResourcePacker.FormatVersion}.");
            }

            // Read resource count
            var count = reader.ReadInt32();

            // Read index entries
            var indexEntries = new List<IndexEntry>();
            for (int i = 0; i < count; i++)
            {
                var key = reader.ReadString();
                var originalLength = reader.ReadInt32();
                var storedLength = reader.ReadInt32();
                var isCompressed = reader.ReadBoolean();
                indexEntries.Add(new IndexEntry(key, originalLength, storedLength, isCompressed));
            }

            // Calculate offsets based on the current position
            long currentOffset = fileStream.Position;
            foreach (var entry in indexEntries)
            {
                _resourceIndex[entry.Key] = new ResourceEntry
                {
                    Offset = currentOffset,
                    Length = entry.StoredLength,
                    OriginalLength = entry.OriginalLength,
                    IsCompressed = entry.IsCompressed
                };
                currentOffset += entry.StoredLength;
            }
        }

        /// <summary>
        /// Gets a list of all available resource keys in the package.
        /// </summary>
        public IEnumerable<string> ResourceKeys => _resourceIndex.Keys;

        /// <summary>
        /// Determines whether the package contains a resource with the specified key.
        /// </summary>
        /// <param name="resourceKey">The key to check for.</param>
        /// <returns>true if the package contains a resource with the specified key; otherwise, false.</returns>
        public bool ContainsKey(string resourceKey)
        {
            return _resourceIndex.ContainsKey(resourceKey);
        }

        /// <summary>
        /// Reads a resource as a byte array asynchronously.
        /// </summary>
        /// <param name="resourceKey">The key of the resource to read.</param>
        /// <returns>A task that represents the asynchronous read operation.</returns>
        public Task<byte[]> ReadResourceAsync(string resourceKey)
        {
            if (!_resourceIndex.TryGetValue(resourceKey, out var entry))
                throw new KeyNotFoundException($"Resource with key '{resourceKey}' not found.");

            var buffer = new byte[entry.Length];
            
            // Create a new FileStream for this read operation (thread-safe via FileShare.Read)
            using (var fileStream = new FileStream(_filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                fileStream.Seek(entry.Offset, SeekOrigin.Begin);
                
                int totalRead = 0;
                while (totalRead < entry.Length)
                {
                    int bytesRead = fileStream.Read(buffer, totalRead, entry.Length - totalRead);
                    if (bytesRead == 0)
                        throw new EndOfStreamException($"Unexpected end of stream while reading resource '{resourceKey}'.");
                    totalRead += bytesRead;
                }
            }

            // Decompress if needed
            if (entry.IsCompressed)
            {
                buffer = DecompressData(buffer);
            }
            
            return Task.FromResult(buffer);
        }

        /// <summary>
        /// Reads a resource as a string asynchronously using UTF-8 encoding.
        /// </summary>
        /// <param name="resourceKey">The key of the resource to read.</param>
        /// <returns>A task that represents the asynchronous read operation.</returns>
        public async Task<string> ReadResourceAsStringAsync(string resourceKey)
        {
            var bytes = await ReadResourceAsync(resourceKey);
            return System.Text.Encoding.UTF8.GetString(bytes);
        }

        /// <summary>
        /// Reads a resource as a string asynchronously using the specified encoding.
        /// </summary>
        /// <param name="resourceKey">The key of the resource to read.</param>
        /// <param name="encoding">The encoding to use when converting bytes to string.</param>
        /// <returns>A task that represents the asynchronous read operation.</returns>
        public async Task<string> ReadResourceAsStringAsync(string resourceKey, Encoding encoding)
        {
            var bytes = await ReadResourceAsync(resourceKey);
            return encoding.GetString(bytes);
        }

        /// <summary>
        /// Reads a resource as a byte array synchronously.
        /// </summary>
        /// <param name="resourceKey">The key of the resource to read.</param>
        /// <returns>The resource data as a byte array.</returns>
        public byte[] ReadResource(string resourceKey)
        {
            if (!_resourceIndex.TryGetValue(resourceKey, out var entry))
                throw new KeyNotFoundException($"Resource with key '{resourceKey}' not found.");

            var buffer = new byte[entry.Length];
            
            // Create a new FileStream for this read operation (thread-safe via FileShare.Read)
            using (var fileStream = new FileStream(_filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                fileStream.Seek(entry.Offset, SeekOrigin.Begin);
                
                int totalRead = 0;
                while (totalRead < entry.Length)
                {
                    int bytesRead = fileStream.Read(buffer, totalRead, entry.Length - totalRead);
                    if (bytesRead == 0)
                        throw new EndOfStreamException($"Unexpected end of stream while reading resource '{resourceKey}'.");
                    totalRead += bytesRead;
                }
            }

            // Decompress if needed
            if (entry.IsCompressed)
            {
                buffer = DecompressData(buffer);
            }
            
            return buffer;
        }

        /// <summary>
        /// Reads a resource as a string synchronously using UTF-8 encoding.
        /// </summary>
        /// <param name="resourceKey">The key of the resource to read.</param>
        /// <returns>The resource data as a string.</returns>
        public string ReadResourceAsString(string resourceKey)
        {
            var bytes = ReadResource(resourceKey);
            return System.Text.Encoding.UTF8.GetString(bytes);
        }

        /// <summary>
        /// Reads a resource as a string synchronously using the specified encoding.
        /// </summary>
        /// <param name="resourceKey">The key of the resource to read.</param>
        /// <param name="encoding">The encoding to use when converting bytes to string.</param>
        /// <returns>The resource data as a string.</returns>
        public string ReadResourceAsString(string resourceKey, Encoding encoding)
        {
            var bytes = ReadResource(resourceKey);
            return encoding.GetString(bytes);
        }

        /// <summary>
        /// Gets a read-only stream for the specified resource.
        /// This allows streaming large resources without loading all data into memory.
        /// </summary>
        /// <param name="resourceKey">The key of the resource to read.</param>
        /// <returns>A read-only stream containing the resource data.</returns>
        public Stream GetStream(string resourceKey)
        {
            if (!_resourceIndex.TryGetValue(resourceKey, out var entry))
                throw new KeyNotFoundException($"Resource with key '{resourceKey}' not found.");

            // For compressed resources, use a decompression stream
            if (entry.IsCompressed)
            {
                return new ResourceDecompressionStream(_filePath, entry.Offset, entry.Length);
            }

            // Create a SubStream for streaming access without loading entire resource into memory
            return new ResourceSubStream(_filePath, entry.Offset, entry.Length);
        }

        /// <summary>
        /// Decompresses data using GZip decompression.
        /// </summary>
        /// <param name="compressedData">The compressed data.</param>
        /// <returns>The decompressed data.</returns>
        private byte[] DecompressData(byte[] compressedData)
        {
            using var inputStream = new MemoryStream(compressedData);
            using var gzipStream = new GZipStream(inputStream, CompressionMode.Decompress);
            using var outputStream = new MemoryStream();
            gzipStream.CopyTo(outputStream);
            return outputStream.ToArray();
        }
    }

    internal readonly struct IndexEntry
    {
        public string Key { get; }
        public int OriginalLength { get; }
        public int StoredLength { get; }
        public bool IsCompressed { get; }

        public IndexEntry(string key, int originalLength, int storedLength, bool isCompressed)
        {
            Key = key;
            OriginalLength = originalLength;
            StoredLength = storedLength;
            IsCompressed = isCompressed;
        }
    }

    internal class ResourceEntry
    {
        public long Offset { get; set; }
        public int Length { get; set; }
        public int OriginalLength { get; set; }
        public bool IsCompressed { get; set; }
    }

    /// <summary>
    /// A read-only stream that provides access to a portion of a resource package file.
    /// This allows streaming large resources without loading all data into memory.
    /// Each instance creates its own FileStream for thread-safe operation.
    /// </summary>
    internal class ResourceSubStream : Stream
    {
        private readonly string _filePath;
        private readonly long _resourceOffset;
        private readonly long _resourceLength;
        private long _position;
        private FileStream? _fileStream;
        private bool _disposed;

        public ResourceSubStream(string filePath, long offset, long length)
        {
            _filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
            _resourceOffset = offset;
            _resourceLength = length;
            _position = 0;
        }

        private FileStream EnsureFileStream()
        {
            if (_fileStream == null)
            {
                _fileStream = new FileStream(_filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            }
            return _fileStream;
        }

        public override bool CanRead => !_disposed;
        public override bool CanSeek => !_disposed;
        public override bool CanWrite => false;
        public override long Length => _resourceLength;

        public override long Position
        {
            get => _position;
            set
            {
                if (_disposed)
                    throw new ObjectDisposedException(nameof(ResourceSubStream));
                if (value < 0 || value > _resourceLength)
                    throw new ArgumentOutOfRangeException(nameof(value));
                _position = value;
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(ResourceSubStream));
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));
            if (offset < 0)
                throw new ArgumentOutOfRangeException(nameof(offset));
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));
            if (buffer.Length - offset < count)
                throw new ArgumentException("Invalid offset/count combination");

            long remaining = _resourceLength - _position;
            if (remaining <= 0)
                return 0;

            int toRead = (int)Math.Min(count, remaining);
            
            var fs = EnsureFileStream();
            fs.Seek(_resourceOffset + _position, SeekOrigin.Begin);
            int bytesRead = fs.Read(buffer, offset, toRead);
            _position += bytesRead;
            
            return bytesRead;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(ResourceSubStream));

            long newPosition = origin switch
            {
                SeekOrigin.Begin => offset,
                SeekOrigin.Current => _position + offset,
                SeekOrigin.End => _resourceLength + offset,
                _ => throw new ArgumentException("Invalid seek origin", nameof(origin))
            };

            if (newPosition < 0 || newPosition > _resourceLength)
                throw new ArgumentOutOfRangeException(nameof(offset));

            _position = newPosition;
            return _position;
        }

        public override void Flush()
        {
            // Read-only stream, nothing to flush
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException("Cannot set length on a read-only stream.");
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException("Cannot write to a read-only stream.");
        }

        protected override void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _fileStream?.Dispose();
                }
                _disposed = true;
            }
            base.Dispose(disposing);
        }
    }

    /// <summary>
    /// A read-only stream that provides decompression on-the-fly for compressed resources.
    /// This allows streaming large compressed resources without loading all data into memory.
    /// </summary>
    internal class ResourceDecompressionStream : Stream
    {
        private readonly GZipStream _gzipStream;
        private readonly Stream _compressedSubStream;
        private bool _disposed;

        public ResourceDecompressionStream(string filePath, long offset, long compressedLength)
        {
            // Create a sub-stream for the compressed data
            _compressedSubStream = new ResourceSubStream(filePath, offset, compressedLength);
            // Wrap it in a GZipStream for decompression
            _gzipStream = new GZipStream(_compressedSubStream, CompressionMode.Decompress, leaveOpen: false);
        }

        public override bool CanRead => !_disposed;
        public override bool CanSeek => false; // GZipStream doesn't support seeking
        public override bool CanWrite => false;
        
        public override long Length => throw new NotSupportedException("Length is not supported for compressed streams.");
        
        public override long Position 
        { 
            get => throw new NotSupportedException("Position is not supported for compressed streams.");
            set => throw new NotSupportedException("Seeking is not supported for compressed streams.");
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(ResourceDecompressionStream));
            
            return _gzipStream.Read(buffer, offset, count);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException("Seeking is not supported for compressed streams.");
        }

        public override void Flush()
        {
            // Read-only stream, nothing to flush
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException("Cannot set length on a read-only stream.");
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException("Cannot write to a read-only stream.");
        }

        protected override void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _gzipStream?.Dispose();
                    // _compressedSubStream will be disposed by _gzipStream since leaveOpen is false
                }
                _disposed = true;
            }
            base.Dispose(disposing);
        }
    }
}