using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace LuYao.ResourcePacker
{
    /// <summary>
    /// Provides functionality to read resources from a packaged resource file.
    /// This class is thread-safe for concurrent read operations.
    /// </summary>
    public class ResourcePackageReader : IDisposable
    {
        private readonly FileStream _fileStream;
        private readonly Dictionary<string, ResourceEntry> _resourceIndex;
        private readonly object _lock = new object();
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
            using var reader = new BinaryReader(_fileStream, System.Text.Encoding.UTF8, leaveOpen: true);
            
            // Read version number
            var version = reader.ReadByte();
            if (version != ResourcePacker.FormatVersion)
            {
                throw new InvalidDataException($"Unsupported file version: {version}. Expected version {ResourcePacker.FormatVersion}.");
            }

            // Read resource count
            var count = reader.ReadInt32();

            // Read index entries (key and length only)
            var indexEntries = new List<(string Key, int Length)>();
            for (int i = 0; i < count; i++)
            {
                var key = reader.ReadString();
                var length = reader.ReadInt32();
                indexEntries.Add((key, length));
            }

            // Calculate offsets based on the current position
            long currentOffset = _fileStream.Position;
            foreach (var (key, length) in indexEntries)
            {
                _resourceIndex[key] = new ResourceEntry
                {
                    Offset = currentOffset,
                    Length = length
                };
                currentOffset += length;
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
            if (_disposed)
                throw new ObjectDisposedException(nameof(ResourcePackageReader));

            if (!_resourceIndex.TryGetValue(resourceKey, out var entry))
                throw new KeyNotFoundException($"Resource with key '{resourceKey}' not found.");

            var buffer = new byte[entry.Length];
            
            // Lock to ensure thread-safe access to FileStream
            lock (_lock)
            {
                _fileStream.Seek(entry.Offset, SeekOrigin.Begin);
                
                int totalRead = 0;
                while (totalRead < entry.Length)
                {
                    int bytesRead = _fileStream.Read(buffer, totalRead, entry.Length - totalRead);
                    if (bytesRead == 0)
                        throw new EndOfStreamException($"Unexpected end of stream while reading resource '{resourceKey}'.");
                    totalRead += bytesRead;
                }
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
            if (_disposed)
                throw new ObjectDisposedException(nameof(ResourcePackageReader));

            if (!_resourceIndex.TryGetValue(resourceKey, out var entry))
                throw new KeyNotFoundException($"Resource with key '{resourceKey}' not found.");

            var buffer = new byte[entry.Length];
            
            // Lock to ensure thread-safe access to FileStream
            lock (_lock)
            {
                _fileStream.Seek(entry.Offset, SeekOrigin.Begin);
                
                int totalRead = 0;
                while (totalRead < entry.Length)
                {
                    int bytesRead = _fileStream.Read(buffer, totalRead, entry.Length - totalRead);
                    if (bytesRead == 0)
                        throw new EndOfStreamException($"Unexpected end of stream while reading resource '{resourceKey}'.");
                    totalRead += bytesRead;
                }
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
        /// </summary>
        /// <param name="resourceKey">The key of the resource to read.</param>
        /// <returns>A read-only stream containing the resource data.</returns>
        public Stream GetStream(string resourceKey)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(ResourcePackageReader));

            if (!_resourceIndex.TryGetValue(resourceKey, out var entry))
                throw new KeyNotFoundException($"Resource with key '{resourceKey}' not found.");

            // Create a SubStream wrapper that provides a read-only view of a portion of the file
            return new SubStream(_fileStream, entry.Offset, entry.Length, _lock);
        }

        /// <summary>
        /// Releases the resources used by the <see cref="ResourcePackageReader"/>.
        /// </summary>
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

    /// <summary>
    /// A stream wrapper that provides a read-only view of a portion of another stream.
    /// This class is thread-safe when used with a lock object.
    /// </summary>
    internal class SubStream : Stream
    {
        private readonly Stream _baseStream;
        private readonly long _offset;
        private readonly long _length;
        private readonly object _lock;
        private long _position;

        public SubStream(Stream baseStream, long offset, long length, object lockObject)
        {
            _baseStream = baseStream ?? throw new ArgumentNullException(nameof(baseStream));
            _lock = lockObject ?? throw new ArgumentNullException(nameof(lockObject));
            _offset = offset;
            _length = length;
            _position = 0;
        }

        public override bool CanRead => true;
        public override bool CanSeek => true;
        public override bool CanWrite => false;
        public override long Length => _length;

        public override long Position
        {
            get => _position;
            set
            {
                if (value < 0 || value > _length)
                    throw new ArgumentOutOfRangeException(nameof(value));
                _position = value;
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));
            if (offset < 0)
                throw new ArgumentOutOfRangeException(nameof(offset));
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));
            if (buffer.Length - offset < count)
                throw new ArgumentException("Invalid offset/count combination");

            long remaining = _length - _position;
            if (remaining <= 0)
                return 0;

            int toRead = (int)Math.Min(count, remaining);
            int bytesRead;
            
            // Lock to ensure thread-safe access to the base stream
            lock (_lock)
            {
                _baseStream.Seek(_offset + _position, SeekOrigin.Begin);
                bytesRead = _baseStream.Read(buffer, offset, toRead);
            }
            
            _position += bytesRead;
            
            return bytesRead;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            long newPosition = origin switch
            {
                SeekOrigin.Begin => offset,
                SeekOrigin.Current => _position + offset,
                SeekOrigin.End => _length + offset,
                _ => throw new ArgumentException("Invalid seek origin", nameof(origin))
            };

            if (newPosition < 0 || newPosition > _length)
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
    }
}