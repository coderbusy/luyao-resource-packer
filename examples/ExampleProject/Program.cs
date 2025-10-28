using LuYao.ResourcePacker;
using System.Text;
using ExampleProject; // Import the namespace where the generated class resides

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("=== LuYao.ResourcePacker Example ===");
        Console.WriteLine();

        // ===== Original API =====
        Console.WriteLine("--- Original API (using ResourcePackageReader directly) ---");
        
        // The .dat file is created during build in the output directory
        var datFilePath = Path.Combine(AppContext.BaseDirectory, "ExampleProject.dat");
        
        if (!File.Exists(datFilePath))
        {
            Console.WriteLine($"Error: Resource package file not found: {datFilePath}");
            Console.WriteLine("Please build the project first to generate the .dat file.");
            return;
        }

        var reader = new ResourcePackageReader(datFilePath);
        
        Console.WriteLine($"Resource package loaded: {datFilePath}");
        Console.WriteLine();

        // List all available resources
        Console.WriteLine("Available resources:");
        foreach (var key in reader.ResourceKeys)
        {
            Console.WriteLine($"  - {key}");
        }
        Console.WriteLine();

        // Read message resource as string
        Console.WriteLine("--- Reading 'message' resource as string ---");
        string message = await reader.ReadResourceAsStringAsync("message");
        Console.WriteLine(message);
        Console.WriteLine();

        // ===== New Strongly-Typed API (Generated) =====
        Console.WriteLine("--- New Strongly-Typed API (using R class) ---");
        Console.WriteLine();
        
        // Access resource keys as constants
        Console.WriteLine("Resource keys from generated class:");
        Console.WriteLine($"  - {R.Keys.config}");
        Console.WriteLine($"  - {R.Keys.message}");
        Console.WriteLine($"  - {R.Keys.template}");
        Console.WriteLine();
        
        // Read resources using generated methods
        Console.WriteLine("--- Reading 'config' using generated method ---");
        string config = await R.ReadConfigAsStringAsync();
        Console.WriteLine(config);
        Console.WriteLine();
        
        Console.WriteLine("--- Reading 'message' using generated method ---");
        string messageGenerated = await R.ReadMessageAsStringAsync();
        Console.WriteLine(messageGenerated);
        Console.WriteLine();
        
        // Read template resource as bytes using generated method
        Console.WriteLine("--- Reading 'template' using generated method ---");
        byte[] templateBytes = await R.ReadTemplateAsync();
        Console.WriteLine($"Template size: {templateBytes.Length} bytes");
        string templateContent = Encoding.UTF8.GetString(templateBytes);
        Console.WriteLine("Template content (first 200 characters):");
        Console.WriteLine(templateContent.Substring(0, Math.Min(200, templateContent.Length)) + "...");
        Console.WriteLine();

        Console.WriteLine("=== Example completed successfully ===");
    }
}