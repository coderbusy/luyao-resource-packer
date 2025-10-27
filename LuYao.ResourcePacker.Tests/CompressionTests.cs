using Xunit;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LuYao.ResourcePacker.Tests
{
    public class CompressionTests : IDisposable
    {
        private readonly string _tempDirectory;

        public CompressionTests()
        {
            _tempDirectory = Path.Combine(Path.GetTempPath(), $"CompressionTests_{Guid.NewGuid()}");
            Directory.CreateDirectory(_tempDirectory);
        }

        [Fact]
        public void SmallFile_ShouldNotBeCompressed()
        {
            // Arrange - Create a file smaller than 255 bytes
            var sourceDir = Path.Combine(_tempDirectory, "source");
            Directory.CreateDirectory(sourceDir);
            var smallFile = Path.Combine(sourceDir, "small.txt");
            var content = "Small content under 255 bytes";
            File.WriteAllText(smallFile, content);

            var outputPath = Path.Combine(_tempDirectory, "test.dat");
            var packer = new ResourcePacker(sourceDir);

            // Act
            packer.PackResources(outputPath);

            // Assert - Read the binary format to check compression flag
            using var fs = new FileStream(outputPath, FileMode.Open, FileAccess.Read);
            using var reader = new BinaryReader(fs);
            
            var version = reader.ReadByte();
            var count = reader.ReadInt32();
            var key = reader.ReadString();
            var originalLength = reader.ReadInt32();
            var storedLength = reader.ReadInt32();
            var isCompressed = reader.ReadBoolean();

            Assert.False(isCompressed, "Small files (<255 bytes) should not be compressed");
            Assert.Equal(originalLength, storedLength);
        }

        [Fact]
        public void MediumFile_WithGoodCompressionRatio_ShouldBeCompressed()
        {
            // Arrange - Create a file between 255 bytes and 4KB with repeating content (good compression)
            var sourceDir = Path.Combine(_tempDirectory, "source");
            Directory.CreateDirectory(sourceDir);
            var mediumFile = Path.Combine(sourceDir, "medium.txt");
            var content = new string('A', 1000); // 1000 bytes of repeated character
            File.WriteAllText(mediumFile, content);

            var outputPath = Path.Combine(_tempDirectory, "test.dat");
            var packer = new ResourcePacker(sourceDir);

            // Act
            packer.PackResources(outputPath);

            // Assert - Read the binary format to check compression
            using var fs = new FileStream(outputPath, FileMode.Open, FileAccess.Read);
            using var reader = new BinaryReader(fs);
            
            var version = reader.ReadByte();
            var count = reader.ReadInt32();
            var key = reader.ReadString();
            var originalLength = reader.ReadInt32();
            var storedLength = reader.ReadInt32();
            var isCompressed = reader.ReadBoolean();

            Assert.True(isCompressed, "Medium files with good compression ratio should be compressed");
            Assert.True(storedLength < originalLength, "Compressed size should be smaller");
            
            // Verify compression ratio is at least 5%
            var compressionRatio = 1.0 - ((double)storedLength / originalLength);
            Assert.True(compressionRatio >= 0.05, $"Compression ratio {compressionRatio:P} should be at least 5%");
        }

        [Fact]
        public void LargeFile_WithGoodCompressionRatio_ShouldBeCompressed()
        {
            // Arrange - Create a file larger than 4KB with repeating content
            var sourceDir = Path.Combine(_tempDirectory, "source");
            Directory.CreateDirectory(sourceDir);
            var largeFile = Path.Combine(sourceDir, "large.txt");
            var content = new string('B', 10000); // 10KB of repeated character
            File.WriteAllText(largeFile, content);

            var outputPath = Path.Combine(_tempDirectory, "test.dat");
            var packer = new ResourcePacker(sourceDir);

            // Act
            packer.PackResources(outputPath);

            // Assert
            using var fs = new FileStream(outputPath, FileMode.Open, FileAccess.Read);
            using var reader = new BinaryReader(fs);
            
            var version = reader.ReadByte();
            var count = reader.ReadInt32();
            var key = reader.ReadString();
            var originalLength = reader.ReadInt32();
            var storedLength = reader.ReadInt32();
            var isCompressed = reader.ReadBoolean();

            Assert.True(isCompressed, "Large files with good compression ratio should be compressed");
            Assert.True(storedLength < originalLength, "Compressed size should be smaller");
        }

        [Fact]
        public void CompressedFileFormats_ShouldNotBeCompressed()
        {
            // Arrange - Create files with common compressed extensions
            var sourceDir = Path.Combine(_tempDirectory, "source");
            Directory.CreateDirectory(sourceDir);
            
            var extensions = new[] { ".jpg", ".png", ".zip", ".gz" };
            foreach (var ext in extensions)
            {
                var filePath = Path.Combine(sourceDir, $"file{ext}");
                // Create a file with 1000 bytes to ensure it's above compression threshold
                File.WriteAllBytes(filePath, Encoding.UTF8.GetBytes(new string('X', 1000)));
            }

            var outputPath = Path.Combine(_tempDirectory, "test.dat");
            var packer = new ResourcePacker(sourceDir);

            // Act
            packer.PackResources(outputPath);

            // Assert - All compressed format files should not be compressed
            using var fs = new FileStream(outputPath, FileMode.Open, FileAccess.Read);
            using var reader = new BinaryReader(fs);
            
            var version = reader.ReadByte();
            var count = reader.ReadInt32();

            for (int i = 0; i < count; i++)
            {
                var key = reader.ReadString();
                var originalLength = reader.ReadInt32();
                var storedLength = reader.ReadInt32();
                var isCompressed = reader.ReadBoolean();

                Assert.False(isCompressed, $"File with key '{key}' should not be compressed (already compressed format)");
            }
        }

        [Fact]
        public async Task CompressedResource_ShouldDecompressCorrectly()
        {
            // Arrange - Create a compressible file
            var sourceDir = Path.Combine(_tempDirectory, "source");
            Directory.CreateDirectory(sourceDir);
            var testFile = Path.Combine(sourceDir, "compressible.txt");
            var originalContent = new string('C', 2000); // 2KB of repeated character
            File.WriteAllText(testFile, originalContent);

            var outputPath = Path.Combine(_tempDirectory, "test.dat");
            var packer = new ResourcePacker(sourceDir);
            packer.PackResources(outputPath);

            // Act - Read the resource
            var packageReader = new ResourcePackageReader(outputPath);
            var readContent = await packageReader.ReadResourceAsStringAsync("compressible");

            // Assert - Content should match original after decompression
            Assert.Equal(originalContent, readContent);
        }

        [Fact]
        public void CompressedResource_SynchronousRead_ShouldDecompressCorrectly()
        {
            // Arrange
            var sourceDir = Path.Combine(_tempDirectory, "source");
            Directory.CreateDirectory(sourceDir);
            var testFile = Path.Combine(sourceDir, "sync_test.txt");
            var originalContent = new string('D', 3000);
            File.WriteAllText(testFile, originalContent);

            var outputPath = Path.Combine(_tempDirectory, "test.dat");
            var packer = new ResourcePacker(sourceDir);
            packer.PackResources(outputPath);

            // Act
            var packageReader = new ResourcePackageReader(outputPath);
            var readContent = packageReader.ReadResourceAsString("sync_test");

            // Assert
            Assert.Equal(originalContent, readContent);
        }

        [Fact]
        public void CompressedResource_GetStream_ShouldDecompressCorrectly()
        {
            // Arrange
            var sourceDir = Path.Combine(_tempDirectory, "source");
            Directory.CreateDirectory(sourceDir);
            var testFile = Path.Combine(sourceDir, "stream_test.txt");
            var originalContent = new string('E', 5000);
            File.WriteAllText(testFile, originalContent);

            var outputPath = Path.Combine(_tempDirectory, "test.dat");
            var packer = new ResourcePacker(sourceDir);
            packer.PackResources(outputPath);

            // Act
            var packageReader = new ResourcePackageReader(outputPath);
            using var stream = packageReader.GetStream("stream_test");
            using var reader = new StreamReader(stream);
            var readContent = reader.ReadToEnd();

            // Assert
            Assert.Equal(originalContent, readContent);
        }

        [Fact]
        public void MixedFiles_ShouldCompressSelectively()
        {
            // Arrange - Create a mix of files
            var sourceDir = Path.Combine(_tempDirectory, "source");
            Directory.CreateDirectory(sourceDir);
            
            // Small file (should not compress)
            File.WriteAllText(Path.Combine(sourceDir, "tiny.txt"), "Small");
            
            // Compressible medium file
            File.WriteAllText(Path.Combine(sourceDir, "medium.txt"), new string('M', 1000));
            
            // Already compressed format
            File.WriteAllBytes(Path.Combine(sourceDir, "image.png"), Encoding.UTF8.GetBytes(new string('I', 1000)));
            
            // Large compressible file
            File.WriteAllText(Path.Combine(sourceDir, "large.txt"), new string('L', 10000));

            var outputPath = Path.Combine(_tempDirectory, "test.dat");
            var packer = new ResourcePacker(sourceDir);

            // Act
            packer.PackResources(outputPath);

            // Assert - Verify compression decisions
            var packageReader = new ResourcePackageReader(outputPath);
            
            // All files should be readable
            Assert.True(packageReader.ContainsKey("tiny"));
            Assert.True(packageReader.ContainsKey("medium"));
            Assert.True(packageReader.ContainsKey("image"));
            Assert.True(packageReader.ContainsKey("large"));

            // Verify content integrity
            Assert.Equal("Small", packageReader.ReadResourceAsString("tiny"));
            Assert.Equal(new string('M', 1000), packageReader.ReadResourceAsString("medium"));
            Assert.Equal(new string('I', 1000), packageReader.ReadResourceAsString("image"));
            Assert.Equal(new string('L', 10000), packageReader.ReadResourceAsString("large"));
        }

        [Fact]
        public void Edge_ExactlyAtThresholds_ShouldHandleCorrectly()
        {
            // Arrange - Create files at exact threshold boundaries
            var sourceDir = Path.Combine(_tempDirectory, "source");
            Directory.CreateDirectory(sourceDir);
            
            // Exactly 254 bytes (just below threshold - should not compress)
            File.WriteAllBytes(Path.Combine(sourceDir, "at254.txt"), new byte[254]);
            
            // Exactly 255 bytes (at threshold - should evaluate for compression)
            File.WriteAllBytes(Path.Combine(sourceDir, "at255.txt"), Encoding.UTF8.GetBytes(new string('A', 255)));
            
            // Exactly 4KB (at threshold - should sample)
            File.WriteAllBytes(Path.Combine(sourceDir, "at4kb.txt"), Encoding.UTF8.GetBytes(new string('B', 4096)));

            var outputPath = Path.Combine(_tempDirectory, "test.dat");
            var packer = new ResourcePacker(sourceDir);

            // Act
            packer.PackResources(outputPath);

            // Assert - All files should be readable
            var packageReader = new ResourcePackageReader(outputPath);
            Assert.True(packageReader.ContainsKey("at254"));
            Assert.True(packageReader.ContainsKey("at255"));
            Assert.True(packageReader.ContainsKey("at4kb"));
        }

        public void Dispose()
        {
            if (Directory.Exists(_tempDirectory))
            {
                Directory.Delete(_tempDirectory, true);
            }
        }
    }
}
