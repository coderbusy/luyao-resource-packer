using LuYao.ResourcePacker;
using System.Text;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("=== LuYao.ResourcePacker Example ===");
        Console.WriteLine();

        // The .dat file is created during build in the output directory
        var datFilePath = Path.Combine(AppContext.BaseDirectory, "ExampleProject.dat");
        
        if (!File.Exists(datFilePath))
        {
            Console.WriteLine($"Error: Resource package file not found: {datFilePath}");
            Console.WriteLine("Please build the project first to generate the .dat file.");
            return;
        }

        using var reader = new ResourcePackageReader(datFilePath);
        
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

        // Read config resource as string
        Console.WriteLine("--- Reading 'config' resource as string ---");
        string config = await reader.ReadResourceAsStringAsync("config");
        Console.WriteLine(config);
        Console.WriteLine();

        // Read template resource as bytes and convert to string
        Console.WriteLine("--- Reading 'template' resource as bytes ---");
        byte[] templateBytes = await reader.ReadResourceAsync("template");
        Console.WriteLine($"Template size: {templateBytes.Length} bytes");
        string templateContent = Encoding.UTF8.GetString(templateBytes);
        Console.WriteLine("Template content (first 200 characters):");
        Console.WriteLine(templateContent.Substring(0, Math.Min(200, templateContent.Length)) + "...");
        Console.WriteLine();

        Console.WriteLine("=== Example completed successfully ===");
    }
}