using LuYao.ResourcePacker;

class Program
{
    static async Task Main(string[] args)
    {
        using var reader = new ResourcePackageReader("ExampleProject.dat");
        
        // Read a resource as string
        string message = await reader.ReadResourceAsStringAsync("message");
        Console.WriteLine($"Message content: {message}");

        // Read a resource as bytes
        byte[] configBytes = await reader.ReadResourceAsync("config");
        Console.WriteLine($"Config size: {configBytes.Length} bytes");

        // List all available resources
        Console.WriteLine("\nAvailable resources:");
        foreach (var key in reader.ResourceKeys)
        {
            Console.WriteLine($"- {key}");
        }
    }
}