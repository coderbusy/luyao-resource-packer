using Xunit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LuYao.ResourcePacker.Tests
{
    public class ResourcePackageReaderThreadSafetyTests : IDisposable
    {
        private readonly string _tempDirectory;
        private readonly string _outputPath;

        public ResourcePackageReaderThreadSafetyTests()
        {
            // Create temporary directory for tests
            _tempDirectory = Path.Combine(Path.GetTempPath(), $"ResourcePackerThreadSafetyTests_{Guid.NewGuid()}");
            Directory.CreateDirectory(_tempDirectory);
            _outputPath = Path.Combine(_tempDirectory, "test.dat");
            
            // Create test resource package
            var sourceDir = Path.Combine(Directory.GetCurrentDirectory(), "TestResources");
            var packer = new ResourcePacker(sourceDir);
            packer.PackResources(_outputPath);
        }

        [Fact]
        public async Task ConcurrentReadResourceAsync_ShouldNotCorruptData()
        {
            // Arrange
            var reader = new ResourcePackageReader(_outputPath);
            const int threadCount = 10;
            const int iterationsPerThread = 50;

            // Act - Read the same resources concurrently from multiple threads
            var tasks = Enumerable.Range(0, threadCount).Select(async _ =>
            {
                for (int i = 0; i < iterationsPerThread; i++)
                {
                    var testContent = await reader.ReadResourceAsStringAsync("test");
                    var greetingContent = await reader.ReadResourceAsStringAsync("greeting");
                    
                    // Assert - Verify data integrity
                    Assert.Contains("Hello, World!", testContent);
                    Assert.Contains("Hello from resource file!", greetingContent);
                }
            }).ToArray();

            await Task.WhenAll(tasks);
        }

        [Fact]
        public void ConcurrentReadResource_ShouldNotCorruptData()
        {
            // Arrange
            var reader = new ResourcePackageReader(_outputPath);
            const int threadCount = 10;
            const int iterationsPerThread = 50;

            // Act - Read the same resources concurrently from multiple threads
            var tasks = Enumerable.Range(0, threadCount).Select(i => Task.Run(() =>
            {
                for (int j = 0; j < iterationsPerThread; j++)
                {
                    var testContent = reader.ReadResourceAsString("test");
                    var greetingContent = reader.ReadResourceAsString("greeting");
                    
                    // Assert - Verify data integrity
                    Assert.Contains("Hello, World!", testContent);
                    Assert.Contains("Hello from resource file!", greetingContent);
                }
            })).ToArray();

            Task.WaitAll(tasks);
        }

        [Fact]
        public void ConcurrentReadResourceBytes_ShouldReturnCorrectData()
        {
            // Arrange
            var reader = new ResourcePackageReader(_outputPath);
            const int threadCount = 10;
            const int iterationsPerThread = 50;

            // Act - Read the same resources concurrently from multiple threads
            var tasks = Enumerable.Range(0, threadCount).Select(i => Task.Run(() =>
            {
                for (int j = 0; j < iterationsPerThread; j++)
                {
                    var testBytes = reader.ReadResource("test");
                    var greetingBytes = reader.ReadResource("greeting");
                    
                    // Assert - Verify data integrity by converting to string
                    var testContent = Encoding.UTF8.GetString(testBytes);
                    var greetingContent = Encoding.UTF8.GetString(greetingBytes);
                    
                    Assert.Contains("Hello, World!", testContent);
                    Assert.Contains("Hello from resource file!", greetingContent);
                }
            })).ToArray();

            Task.WaitAll(tasks);
        }

        [Fact]
        public void ConcurrentGetStream_ShouldNotCorruptData()
        {
            // Arrange
            var reader = new ResourcePackageReader(_outputPath);
            const int threadCount = 10;
            const int iterationsPerThread = 20;

            // Act - Get streams concurrently from multiple threads
            var tasks = Enumerable.Range(0, threadCount).Select(i => Task.Run(() =>
            {
                for (int j = 0; j < iterationsPerThread; j++)
                {
                    using var testStream = reader.GetStream("test");
                    using var greetingStream = reader.GetStream("greeting");
                    
                    using var testReader = new StreamReader(testStream);
                    using var greetingReader = new StreamReader(greetingStream);
                    
                    var testContent = testReader.ReadToEnd();
                    var greetingContent = greetingReader.ReadToEnd();
                    
                    // Assert - Verify data integrity
                    Assert.Contains("Hello, World!", testContent);
                    Assert.Contains("Hello from resource file!", greetingContent);
                }
            })).ToArray();

            Task.WaitAll(tasks);
        }

        [Fact]
        public void MixedConcurrentOperations_ShouldNotCorruptData()
        {
            // Arrange
            var reader = new ResourcePackageReader(_outputPath);
            const int threadCount = 15;
            const int iterationsPerThread = 30;

            // Act - Mix different read operations concurrently
            var tasks = Enumerable.Range(0, threadCount).Select(i => Task.Run(() =>
            {
                for (int j = 0; j < iterationsPerThread; j++)
                {
                    // Alternate between different read methods
                    switch (j % 3)
                    {
                        case 0:
                            var bytes = reader.ReadResource("test");
                            Assert.True(bytes.Length > 0);
                            break;
                        case 1:
                            var content = reader.ReadResourceAsString("greeting");
                            Assert.Contains("Hello from resource file!", content);
                            break;
                        case 2:
                            using (var stream = reader.GetStream("test"))
                            {
                                using var sr = new StreamReader(stream);
                                var streamContent = sr.ReadToEnd();
                                Assert.Contains("Hello, World!", streamContent);
                            }
                            break;
                    }
                }
            })).ToArray();

            Task.WaitAll(tasks);
        }

        [Fact]
        public async Task ConcurrentReadWithDifferentEncodings_ShouldNotCorruptData()
        {
            // Arrange
            var reader = new ResourcePackageReader(_outputPath);
            const int threadCount = 8;
            const int iterationsPerThread = 40;

            // Act - Read with different encodings concurrently
            var tasks = Enumerable.Range(0, threadCount).Select(async i =>
            {
                for (int j = 0; j < iterationsPerThread; j++)
                {
                    var encoding = (j % 2 == 0) ? Encoding.UTF8 : Encoding.ASCII;
                    var content = await reader.ReadResourceAsStringAsync("test", encoding);
                    
                    // Assert - Verify data integrity
                    Assert.Contains("Hello, World!", content);
                }
            }).ToArray();

            await Task.WhenAll(tasks);
        }

        [Fact]
        public void ConcurrentStreamReads_ShouldNotCorruptData()
        {
            // Arrange
            var reader = new ResourcePackageReader(_outputPath);
            const int threadCount = 10;

            // Act - Multiple threads reading from streams simultaneously
            var tasks = Enumerable.Range(0, threadCount).Select(i => Task.Run(() =>
            {
                using var stream = reader.GetStream("test");
                var buffer = new byte[1024];
                var totalRead = 0;
                
                while (true)
                {
                    var bytesRead = stream.Read(buffer, totalRead, buffer.Length - totalRead);
                    if (bytesRead == 0)
                        break;
                    totalRead += bytesRead;
                }
                
                var content = Encoding.UTF8.GetString(buffer, 0, totalRead);
                
                // Assert - Verify data integrity
                Assert.Contains("Hello, World!", content);
            })).ToArray();

            Task.WaitAll(tasks);
        }

        public void Dispose()
        {
            // Clean up temporary directory
            if (Directory.Exists(_tempDirectory))
            {
                Directory.Delete(_tempDirectory, true);
            }
        }
    }
}
