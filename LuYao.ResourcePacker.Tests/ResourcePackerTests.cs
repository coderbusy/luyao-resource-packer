using Xunit;
using System.IO;
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

            using var reader = new ResourcePackageReader(_outputPath);
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
            using var reader = new ResourcePackageReader(_outputPath);
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
            using var reader = new ResourcePackageReader(_outputPath);
            await Assert.ThrowsAsync<KeyNotFoundException>(() => 
                reader.ReadResourceAsync("non_existent_key"));
        }

        [Fact]
        public async Task ReadResource_AfterDispose_ShouldThrowException()
        {
            // Arrange
            var sourceDir = Path.Combine(Directory.GetCurrentDirectory(), "TestResources");
            var packer = new ResourcePacker(sourceDir, "*.res.*");
            packer.PackResources(_outputPath);

            // Act
            var reader = new ResourcePackageReader(_outputPath);
            reader.Dispose();

            // Assert
            await Assert.ThrowsAsync<ObjectDisposedException>(() => 
                reader.ReadResourceAsync("test"));
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
            using var reader = new ResourcePackageReader(_outputPath);
            Assert.Empty(reader.ResourceKeys);
        }

        [Theory]
        [InlineData(null, "*.res.*")]
        [InlineData("", "*.res.*")]
        [InlineData("C:\\SomeDir", null)]
        [InlineData("C:\\SomeDir", "")]
        public void Constructor_WithInvalidParameters_ShouldThrowArgumentException(
            string sourceDirectory, 
            string pattern)
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new ResourcePacker(sourceDirectory, pattern));
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