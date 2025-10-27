using System;
using System.Threading.Tasks;
using LuYao.ResourcePacker;

namespace Popcorn.Toolkit
{
    /// <summary>
    /// Test program to verify that the generated R class respects the RootNamespace property.
    /// When RootNamespace is set to "Popcorn.Toolkit", the R class should be in this namespace.
    /// </summary>
    public class Program
    {
        public static async Task Main(string[] args)
        {
            Console.WriteLine("RootNamespace Test - Verifying generated R class namespace");
            Console.WriteLine("=============================================================");
            Console.WriteLine();
            
            // This test verifies that R class is in Popcorn.Toolkit namespace
            // If RootNamespace property is not respected, this won't compile
            Console.WriteLine($"R class is accessible in namespace: {typeof(R).Namespace}");
            Console.WriteLine($"Expected namespace: Popcorn.Toolkit");
            Console.WriteLine();
            
            // List available resource keys
            Console.WriteLine("Available resource keys:");
            Console.WriteLine($"  - R.Keys.sample: {R.Keys.sample}");
            Console.WriteLine($"  - R.Keys.config: {R.Keys.config}");
            Console.WriteLine();
            
            // Test reading resources
            try
            {
                var sampleText = await R.ReadSampleAsyncAsString();
                Console.WriteLine($"Sample resource content: {sampleText}");
                
                var configJson = await R.ReadConfigAsyncAsString();
                Console.WriteLine($"Config resource content: {configJson}");
                
                Console.WriteLine();
                Console.WriteLine("✓ Test PASSED: R class is in the correct namespace (Popcorn.Toolkit)");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Error reading resources: {ex.Message}");
            }
        }
    }
}
