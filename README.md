# LuYao.ResourcePacker

LuYao.ResourcePacker is a .NET library for packaging and accessing resource files during build time and runtime.

## Features

- Pack multiple resource files into a single .dat file during build
- Resource file scanning based on configurable patterns (default: *.res.*)
- MSBuild integration
- Simple runtime API for resource access
- Async support
- Configurable through MSBuild properties

## Installation

Install the NuGet package:

```bash
dotnet add package LuYao.ResourcePacker
```

## Usage

1. Mark your resource files with the `.res.` pattern:
```
Resources/
  ├── message.res.json
  ├── config.res.txt
  └── template.res.html
```

2. The resources will be automatically packed into a .dat file during build.

3. Access resources at runtime:

```csharp
using LuYao.ResourcePacker;

// Read resources
using var reader = new ResourcePackageReader("YourAssembly.dat");

// Read as string
string content = await reader.ReadResourceAsStringAsync("message");

// Read as bytes
byte[] data = await reader.ReadResourceAsync("config");

// List all resources
foreach (var key in reader.ResourceKeys)
{
    Console.WriteLine(key);
}
```

## Configuration

In your .csproj file:

```xml
<PropertyGroup>
    <!-- Enable/disable resource packing -->
    <ResourcePackerEnabled>true</ResourcePackerEnabled>
    
    <!-- Custom file pattern -->
    <ResourcePackerPattern>*.res.*</ResourcePackerPattern>
    
    <!-- Custom output filename -->
    <ResourcePackerOutputFileName>$(AssemblyName).dat</ResourcePackerOutputFileName>
</PropertyGroup>
```

## Building

```bash
dotnet build
```

## Running Tests

```bash
dotnet test
```

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Created By

Created by Soar360 on 2025-10-25