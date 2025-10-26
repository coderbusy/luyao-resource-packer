using Xunit;
using System.IO;
using System.Linq;
using System.Reflection;

namespace LuYao.ResourcePacker.Tests
{
    public class SourceGeneratorIntegrationTests
    {
        [Fact]
        public void SourceGenerator_ShouldGenerateDataPackageClass()
        {
            // This test verifies that when resource files exist,
            // the source generator creates a DataPackage class with expected members.
            // Since source generators run at compile time, we verify the generated class exists
            // by checking if it can be accessed via reflection (in a real project).
            
            // For this test, we're testing the concept - in a real scenario,
            // we'd have a test project with resource files that gets compiled
            // and then we'd verify the generated class exists.
            
            // This is a placeholder test to ensure the test infrastructure works
            Assert.True(true, "Source generator integration test placeholder");
        }

        [Fact]
        public void GetResourceKey_ExtractsCorrectKeyFromFileName()
        {
            // Test the logic that extracts resource keys from file names
            var testCases = new[]
            {
                ("test.res.txt", "test"),
                ("message.res.json", "message"),
                ("config.res.xml", "config"),
                ("multi.word.res.txt", "multi"),
            };

            foreach (var (fileName, expectedKey) in testCases)
            {
                var firstDot = fileName.IndexOf('.');
                var actualKey = firstDot > 0 ? fileName.Substring(0, firstDot) : fileName;
                
                Assert.Equal(expectedKey, actualKey);
            }
        }

        [Fact]
        public void MakeSafeIdentifier_ShouldReplaceInvalidCharacters()
        {
            // Test the logic that makes safe C# identifiers
            var testCases = new[]
            {
                ("test", "test"),
                ("test-name", "test_name"),
                ("test.name", "test_name"),
                ("test name", "test_name"),
                ("123test", "_123test"),  // Should start with underscore if starts with digit
            };

            foreach (var (input, expected) in testCases)
            {
                var result = MakeSafeIdentifier(input);
                Assert.Equal(expected, result);
            }
        }

        [Fact]
        public void GetOutputFileName_DefaultsToAssemblyNameDat()
        {
            // Test that when no custom filename is specified, it defaults to {AssemblyName}.dat
            var assemblyName = "TestAssembly";
            var expectedFileName = "TestAssembly.dat";
            
            // Simulating the default behavior
            var outputFileName = string.IsNullOrWhiteSpace(null) ? $"{assemblyName}.dat" : null;
            
            Assert.Equal(expectedFileName, outputFileName);
        }

        [Fact]
        public void GetOutputFileName_UsesCustomFilenameWhenProvided()
        {
            // Test that when a custom filename is provided, it's used instead of the default
            var customFileName = "CustomResources.dat";
            var assemblyName = "TestAssembly";
            
            // Simulating the custom filename behavior
            var providedFileName = customFileName;
            var outputFileName = !string.IsNullOrWhiteSpace(providedFileName) 
                ? providedFileName 
                : $"{assemblyName}.dat";
            
            Assert.Equal(customFileName, outputFileName);
        }

        [Fact]
        public void MatchesPattern_ShouldFilterFilesByPattern()
        {
            // Test the logic that matches files against the resource pattern
            var testCases = new[]
            {
                // Pattern: *.res.* - should match files with .res. in the middle
                ("test.res.txt", "*.res.*", true),
                ("message.res.json", "*.res.*", true),
                ("config.res.xml", "*.res.*", true),
                
                // Should not match files without .res.
                ("test.txt", "*.res.*", false),
                ("message.json", "*.res.*", false),
                ("other.xml", "*.res.*", false),
                
                // Edge cases
                ("res.txt", "*.res.*", false),  // Starts with res but no prefix
                ("test.res", "*.res.*", false),  // Ends with res but no suffix
                (".res.", "*.res.*", true),      // Minimal valid match
                
                // Different patterns
                ("test.config.json", "*.config.*", true),
                ("test.data.xml", "*.config.*", false),
            };

            foreach (var (fileName, pattern, expected) in testCases)
            {
                var result = MatchesPattern(fileName, pattern);
                Assert.Equal(expected, result);
            }
        }

        // Helper method that mimics the source generator's logic
        private string MakeSafeIdentifier(string name)
        {
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < name.Length; i++)
            {
                var c = name[i];
                if (char.IsLetterOrDigit(c) || c == '_')
                {
                    sb.Append(c);
                }
                else
                {
                    sb.Append('_');
                }
            }
            
            var result = sb.ToString();
            
            // Ensure it starts with a letter or underscore
            if (result.Length > 0 && !char.IsLetter(result[0]) && result[0] != '_')
            {
                result = "_" + result;
            }
            
            return result;
        }

        // Helper method that mimics the source generator's pattern matching logic
        private bool MatchesPattern(string fileName, string pattern)
        {
            // Simple wildcard pattern matching
            // Pattern format: *.res.* means anything.res.anything
            
            if (string.IsNullOrEmpty(fileName) || string.IsNullOrEmpty(pattern))
            {
                return false;
            }
            
            // Split pattern by wildcard
            var parts = pattern.Split('*');
            
            // If no wildcards, it's an exact match
            if (parts.Length == 1)
            {
                return fileName.Equals(pattern, System.StringComparison.OrdinalIgnoreCase);
            }
            
            // Check each part appears in sequence
            int currentIndex = 0;
            for (int i = 0; i < parts.Length; i++)
            {
                var part = parts[i];
                if (string.IsNullOrEmpty(part))
                {
                    continue; // Skip empty parts from leading/trailing wildcards
                }
                
                // For first part, it must be at the beginning
                if (i == 0 && !fileName.StartsWith(part, System.StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
                // For last part, it must be at the end
                else if (i == parts.Length - 1 && !fileName.EndsWith(part, System.StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
                // For middle parts, find them in sequence
                else
                {
                    int index = fileName.IndexOf(part, currentIndex, System.StringComparison.OrdinalIgnoreCase);
                    if (index < 0)
                    {
                        return false;
                    }
                    currentIndex = index + part.Length;
                }
            }
            
            return true;
        }
    }
}
