using LuYao.ResourcePacker;

// This example demonstrates using LuYao.ResourcePacker.MSBuild via NuGet package reference
// The .dat file is automatically generated during build

var datFile = Path.Combine(AppContext.BaseDirectory, "NuGetReferenceExample.dat");

if (!File.Exists(datFile))
{
    Console.WriteLine($"Error: DAT file not found at {datFile}");
    return 1;
}

Console.WriteLine($"Reading resources from: {datFile}");
Console.WriteLine();

using var reader = new ResourcePackageReader(datFile);

// List all resources
Console.WriteLine("Available resources:");
foreach (var key in reader.ResourceKeys)
{
    Console.WriteLine($"  - {key}");
}
Console.WriteLine();

// Read message resource
var message = await reader.ReadResourceAsStringAsync("message");
Console.WriteLine($"Message content:");
Console.WriteLine(message);
Console.WriteLine();

// Read config resource
var config = await reader.ReadResourceAsStringAsync("config");
Console.WriteLine($"Config content:");
Console.WriteLine(config);

return 0;
