using Xunit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LuYao.ResourcePacker.Tests
{
    public class ResourcePackerTests : IDisposable
    {
        private readonly string _tempDirectory;
        private readonly string _outputPath;

        public ResourcePackerTests()
        {
            // 创建临时目录用于测试
            _tempDirectory = Path.Combine(Path.GetTempPath(), $"ResourcePackerTests_{Guid.NewGuid()}");
            Directory.CreateDirectory(_tempDirectory);
            _outputPath = Path.Combine(_tempDirectory, "test.dat");
        }

        [Fact]
        public async Task PackAndReadResources_ShouldWorkCorrectly()
        {
            // Arrange
            var sourceDir = Path.Combine(Directory.GetCurrentDirectory(), "TestResources");
            var packer = new ResourcePacker(sourceDir, "*.res.*");

            // Act
            packer.PackResources(_outputPath);

            // Assert
            Assert.True(File.Exists(_outputPath));

            var reader = new ResourcePackageReader(_outputPath);
            var jsonContent = await reader.ReadResourceAsStringAsync("test");
            var txtContent = await reader.ReadResourceAsStringAsync("greeting");

            Assert.Contains("Hello, World!", jsonContent);
            Assert.Contains("Hello from resource file!", txtContent);
        }

        [Fact]
        public async Task ResourceKeys_ShouldMatchSourceFiles()
        {
            // Arrange
            var sourceDir = Path.Combine(Directory.GetCurrentDirectory(), "TestResources");
            var packer = new ResourcePacker(sourceDir, "*.res.*");

            // Act
            packer.PackResources(_outputPath);

            // Assert
            var reader = new ResourcePackageReader(_outputPath);
            var keys = reader.ResourceKeys.ToList();

            Assert.Contains("test", keys);
            Assert.Contains("greeting", keys);
            Assert.Equal(2, keys.Count);
        }

        [Fact]
        public async Task ReadResource_WithInvalidKey_ShouldThrowException()
        {
            // Arrange
            var sourceDir = Path.Combine(Directory.GetCurrentDirectory(), "TestResources");
            var packer = new ResourcePacker(sourceDir, "*.res.*");
            packer.PackResources(_outputPath);

            // Act & Assert
            var reader = new ResourcePackageReader(_outputPath);
            await Assert.ThrowsAsync<KeyNotFoundException>(() => 
                reader.ReadResourceAsync("non_existent_key"));
        }

        [Fact]
        public void PackResources_WithEmptyDirectory_ShouldCreateEmptyPackage()
        {
            // Arrange
            var emptyDir = Path.Combine(_tempDirectory, "empty");
            Directory.CreateDirectory(emptyDir);
            var packer = new ResourcePacker(emptyDir, "*.res.*");

            // Act
            packer.PackResources(_outputPath);

            // Assert
            var reader = new ResourcePackageReader(_outputPath);
            Assert.Empty(reader.ResourceKeys);
        }

        [Fact]
        public void PackResources_MultipleTimes_ShouldProduceDeterministicOutput()
        {
            // Arrange
            var sourceDir = Path.Combine(Directory.GetCurrentDirectory(), "TestResources");
            var outputPath1 = Path.Combine(_tempDirectory, "test1.dat");
            var outputPath2 = Path.Combine(_tempDirectory, "test2.dat");
            
            var packer1 = new ResourcePacker(sourceDir, "*.res.*");
            var packer2 = new ResourcePacker(sourceDir, "*.res.*");

            // Act
            packer1.PackResources(outputPath1);
            packer2.PackResources(outputPath2);

            // Assert - Files should be byte-for-byte identical
            var bytes1 = File.ReadAllBytes(outputPath1);
            var bytes2 = File.ReadAllBytes(outputPath2);
            Assert.Equal(bytes1, bytes2);
        }

        [Fact]
        public async Task ResourceKeys_ShouldBeSorted()
        {
            // Arrange
            var sourceDir = Path.Combine(Directory.GetCurrentDirectory(), "TestResources");
            var packer = new ResourcePacker(sourceDir, "*.res.*");

            // Act
            packer.PackResources(_outputPath);

            // Assert
            var reader = new ResourcePackageReader(_outputPath);
            var keys = reader.ResourceKeys.ToList();
            var sortedKeys = keys.OrderBy(k => k).ToList();
            
            Assert.Equal(sortedKeys, keys);
        }

        [Fact]
        public void PackedFile_ShouldHaveCorrectFormat()
        {
            // Arrange
            var sourceDir = Path.Combine(Directory.GetCurrentDirectory(), "TestResources");
            var packer = new ResourcePacker(sourceDir, "*.res.*");

            // Act
            packer.PackResources(_outputPath);

            // Assert - Verify binary format
            using var fs = new FileStream(_outputPath, FileMode.Open, FileAccess.Read);
            using var reader = new BinaryReader(fs);
            
            // First byte should be version 1
            var version = reader.ReadByte();
            Assert.Equal((byte)1, version);
            
            // Next 4 bytes should be resource count
            var count = reader.ReadInt32();
            Assert.True(count > 0, "Should have at least one resource");
            
            // Read index entries - should be sorted
            var keys = new System.Collections.Generic.List<string>();
            for (int i = 0; i < count; i++)
            {
                var key = reader.ReadString();
                var length = reader.ReadInt32();
                keys.Add(key);
                Assert.True(length > 0, $"Resource '{key}' should have positive length");
            }
            
            // Verify keys are sorted
            var sortedKeys = keys.OrderBy(k => k).ToList();
            Assert.Equal(sortedKeys, keys);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void Constructor_WithInvalidParameters_ShouldThrowArgumentException(
            string sourceDirectory)
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new ResourcePacker(sourceDirectory));
        }

        [Fact]
        public async Task ReadResourceAsStringAsync_WithEncoding_ShouldUseProvidedEncoding()
        {
            // Arrange
            var sourceDir = Path.Combine(Directory.GetCurrentDirectory(), "TestResources");
            var packer = new ResourcePacker(sourceDir, "*.res.*");
            packer.PackResources(_outputPath);

            // Act
            var reader = new ResourcePackageReader(_outputPath);
            var content = await reader.ReadResourceAsStringAsync("greeting", Encoding.UTF8);

            // Assert
            Assert.Contains("Hello from resource file!", content);
        }

        [Fact]
        public void ReadResource_ShouldReturnByteArraySynchronously()
        {
            // Arrange
            var sourceDir = Path.Combine(Directory.GetCurrentDirectory(), "TestResources");
            var packer = new ResourcePacker(sourceDir, "*.res.*");
            packer.PackResources(_outputPath);

            // Act
            var reader = new ResourcePackageReader(_outputPath);
            var bytes = reader.ReadResource("greeting");

            // Assert
            Assert.NotNull(bytes);
            Assert.True(bytes.Length > 0);
            var content = Encoding.UTF8.GetString(bytes);
            Assert.Contains("Hello from resource file!", content);
        }

        [Fact]
        public void ReadResourceAsString_ShouldReturnStringSynchronously()
        {
            // Arrange
            var sourceDir = Path.Combine(Directory.GetCurrentDirectory(), "TestResources");
            var packer = new ResourcePacker(sourceDir, "*.res.*");
            packer.PackResources(_outputPath);

            // Act
            var reader = new ResourcePackageReader(_outputPath);
            var content = reader.ReadResourceAsString("greeting");

            // Assert
            Assert.Contains("Hello from resource file!", content);
        }

        [Fact]
        public void ReadResourceAsString_WithEncoding_ShouldUseProvidedEncoding()
        {
            // Arrange
            var sourceDir = Path.Combine(Directory.GetCurrentDirectory(), "TestResources");
            var packer = new ResourcePacker(sourceDir, "*.res.*");
            packer.PackResources(_outputPath);

            // Act
            var reader = new ResourcePackageReader(_outputPath);
            var content = reader.ReadResourceAsString("test", Encoding.UTF8);

            // Assert
            Assert.Contains("Hello, World!", content);
        }

        [Fact]
        public void GetStream_ShouldReturnReadOnlyStream()
        {
            // Arrange
            var sourceDir = Path.Combine(Directory.GetCurrentDirectory(), "TestResources");
            var packer = new ResourcePacker(sourceDir, "*.res.*");
            packer.PackResources(_outputPath);

            // Act
            var reader = new ResourcePackageReader(_outputPath);
            using var stream = reader.GetStream("greeting");

            // Assert
            Assert.NotNull(stream);
            Assert.True(stream.CanRead);
            Assert.False(stream.CanWrite);
            
            using var streamReader = new StreamReader(stream);
            var content = streamReader.ReadToEnd();
            Assert.Contains("Hello from resource file!", content);
        }

        [Fact]
        public void GetStream_WithInvalidKey_ShouldThrowException()
        {
            // Arrange
            var sourceDir = Path.Combine(Directory.GetCurrentDirectory(), "TestResources");
            var packer = new ResourcePacker(sourceDir, "*.res.*");
            packer.PackResources(_outputPath);

            // Act & Assert
            var reader = new ResourcePackageReader(_outputPath);
            Assert.Throws<KeyNotFoundException>(() => 
                reader.GetStream("non_existent_key"));
        }

        [Fact]
        public void ContainsKey_WithExistingKey_ShouldReturnTrue()
        {
            // Arrange
            var sourceDir = Path.Combine(Directory.GetCurrentDirectory(), "TestResources");
            var packer = new ResourcePacker(sourceDir, "*.res.*");
            packer.PackResources(_outputPath);

            // Act
            var reader = new ResourcePackageReader(_outputPath);
            var exists = reader.ContainsKey("test");

            // Assert
            Assert.True(exists);
        }

        [Fact]
        public void ContainsKey_WithNonExistentKey_ShouldReturnFalse()
        {
            // Arrange
            var sourceDir = Path.Combine(Directory.GetCurrentDirectory(), "TestResources");
            var packer = new ResourcePacker(sourceDir, "*.res.*");
            packer.PackResources(_outputPath);

            // Act
            var reader = new ResourcePackageReader(_outputPath);
            var exists = reader.ContainsKey("non_existent_key");

            // Assert
            Assert.False(exists);
        }

        public void Dispose()
        {
            // 清理临时目录
            if (Directory.Exists(_tempDirectory))
            {
                Directory.Delete(_tempDirectory, true);
            }
        }
    }
}