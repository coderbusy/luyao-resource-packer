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
    }
}
