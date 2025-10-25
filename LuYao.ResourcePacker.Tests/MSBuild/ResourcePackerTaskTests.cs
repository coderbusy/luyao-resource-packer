using Xunit;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System.IO;
using LuYao.ResourcePacker.MSBuild;
using System.Collections.Generic;

namespace LuYao.ResourcePacker.Tests.MSBuild
{
    public class ResourcePackerTaskTests : IDisposable
    {
        private readonly string _tempDirectory;
        private readonly List<BuildErrorEventArgs> _errors;
        private readonly List<BuildMessageEventArgs> _messages;
        private readonly MockBuildEngine _mockBuildEngine;

        public ResourcePackerTaskTests()
        {
            _tempDirectory = Path.Combine(Path.GetTempPath(), $"ResourcePackerTaskTests_{Guid.NewGuid()}");
            Directory.CreateDirectory(_tempDirectory);
            
            _errors = new List<BuildErrorEventArgs>();
            _messages = new List<BuildMessageEventArgs>();
            _mockBuildEngine = new MockBuildEngine(_errors, _messages);
        }

        [Fact]
        public void Execute_WithValidParameters_ShouldSucceed()
        {
            // Arrange
            var sourceDir = Path.Combine(_tempDirectory, "source");
            var outputDir = Path.Combine(_tempDirectory, "output");
            Directory.CreateDirectory(sourceDir);
            Directory.CreateDirectory(outputDir);

            // 创建测试资源文件
            File.WriteAllText(
                Path.Combine(sourceDir, "test.res.txt"), 
                "Test Content"
            );

            var task = new ResourcePackerTask
            {
                BuildEngine = _mockBuildEngine,
                ProjectDir = sourceDir,
                OutputPath = outputDir,
                AssemblyName = "TestAssembly",
                ResourcePattern = "*.res.*"
            };

            // Act
            bool result = task.Execute();

            // Assert
            Assert.True(result);
            Assert.Empty(_errors);
            Assert.True(File.Exists(Path.Combine(outputDir, "TestAssembly.dat")));
        }

        [Fact]
        public void Execute_WithInvalidOutputPath_ShouldFail()
        {
            // Arrange
            var task = new ResourcePackerTask
            {
                BuildEngine = _mockBuildEngine,
                ProjectDir = "C:\\NonExistentDir",
                OutputPath = "C:\\NonExistentDir",
                AssemblyName = "TestAssembly",
                ResourcePattern = "*.res.*"
            };

            // Act
            bool result = task.Execute();

            // Assert
            Assert.False(result);
            Assert.NotEmpty(_errors);
        }

        [Fact]
        public void Execute_WithCustomOutputFileName_ShouldUseCustomName()
        {
            // Arrange
            var sourceDir = Path.Combine(_tempDirectory, "source");
            var outputDir = Path.Combine(_tempDirectory, "output");
            Directory.CreateDirectory(sourceDir);
            Directory.CreateDirectory(outputDir);

            var task = new ResourcePackerTask
            {
                BuildEngine = _mockBuildEngine,
                ProjectDir = sourceDir,
                OutputPath = outputDir,
                AssemblyName = "TestAssembly",
                ResourcePattern = "*.res.*",
                OutputFileName = "Custom.dat"
            };

            // Act
            bool result = task.Execute();

            // Assert
            Assert.True(result);
            Assert.True(File.Exists(Path.Combine(outputDir, "Custom.dat")));
        }

        public void Dispose()
        {
            if (Directory.Exists(_tempDirectory))
            {
                Directory.Delete(_tempDirectory, true);
            }
        }

        private class MockBuildEngine : IBuildEngine
        {
            private readonly List<BuildErrorEventArgs> _errors;
            private readonly List<BuildMessageEventArgs> _messages;

            public MockBuildEngine(
                List<BuildErrorEventArgs> errors, 
                List<BuildMessageEventArgs> messages)
            {
                _errors = errors;
                _messages = messages;
            }

            public void LogErrorEvent(BuildErrorEventArgs e) => _errors.Add(e);
            public void LogWarningEvent(BuildWarningEventArgs e) { }
            public void LogMessageEvent(BuildMessageEventArgs e) => _messages.Add(e);
            public void LogCustomEvent(CustomBuildEventArgs e) { }
            public bool ContinueOnError => false;
            public bool BuildProjectFile(
                string projectFileName, 
                string[] targetNames, 
                System.Collections.IDictionary globalProperties, 
                System.Collections.IDictionary targetOutputs) => true;
            public int ColumnNumberOfTaskNode => 0;
            public bool IsRunningMultipleNodes => false;
            public int LineNumberOfTaskNode => 0;
            public string ProjectFileOfTaskNode => string.Empty;
        }
    }
}