# LuYao.ResourcePacker

[![NuGet Version](https://img.shields.io/nuget/v/LuYao.ResourcePacker)](https://www.nuget.org/packages/LuYao.ResourcePacker/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/LuYao.ResourcePacker)](https://www.nuget.org/packages/LuYao.ResourcePacker/)
[![GitHub Stars](https://img.shields.io/github/stars/coderbusy/luyao-resource-packer?style=social)](https://github.com/coderbusy/luyao-resource-packer/stargazers)

LuYao.ResourcePacker is a .NET library for packaging and accessing resource files during build time and runtime.

## Features

- Pack multiple resource files into a single .dat file during build
- Resource file scanning based on configurable patterns (default: *.res.*)
- MSBuild integration
- Simple runtime API for resource access
- Async support
- Configurable through MSBuild properties
- **NEW: C# Source Generator for strongly-typed resource access**

## Installation

### Package Manager Console
```
Install-Package LuYao.ResourcePacker.MSBuild
```

### .NET CLI
```bash
dotnet add package LuYao.ResourcePacker.MSBuild
```

### PackageReference
```xml
<PackageReference Include="LuYao.ResourcePacker.MSBuild" Version="0.1.1" />
```

> **Note**: Installing `LuYao.ResourcePacker.MSBuild` will automatically include the core `LuYao.ResourcePacker` library and the source generator as dependencies.

## Usage

### 1. Basic Setup

Mark your resource files with the `.res.` pattern:
```
Resources/
  ├── message.res.json
  ├── config.res.txt
  └── template.res.html
```

The resources will be automatically packed into a .dat file during build.

### 2. Runtime Access - Original API

Access resources at runtime using the `ResourcePackageReader`:

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

### 3. Runtime Access - Strongly-Typed API (NEW)

The source generator automatically creates a static class named `{AssemblyName}ResourceAccess` in the project's root namespace with strongly-typed access to your resources:

```csharp
using LuYao.ResourcePacker;
using YourAssembly; // Import the namespace where the generated class resides

// Access resource keys as constants
Console.WriteLine(YourAssemblyResourceAccess.Keys.message);
Console.WriteLine(YourAssemblyResourceAccess.Keys.config);
Console.WriteLine(YourAssemblyResourceAccess.Keys.template);

// Read resources using generated methods
string message = await YourAssemblyResourceAccess.ReadMessageAsyncAsString();
byte[] configBytes = await YourAssemblyResourceAccess.ReadConfigAsync();
string template = await YourAssemblyResourceAccess.ReadTemplateAsyncAsString();

// Access the underlying reader if needed
ResourcePackageReader reader = YourAssemblyResourceAccess.Reader;
```

**Benefits of the Strongly-Typed API:**
- IntelliSense support for resource names
- Compile-time checking of resource names
- No magic strings in your code
- Auto-generated documentation comments

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

## How the Source Generator Works

When you add resource files (e.g., `test.res.txt`, `config.res.json`) to your project:

1. During build, the MSBuild task scans for files matching the pattern `*.res.*`
2. These files are packaged into a `.dat` file
3. The source generator creates a static class in the project's root namespace (defaults to assembly name) with:
   - A nested `Keys` class containing const strings for each resource
   - A static `Reader` property providing access to the `ResourcePackageReader`
   - Strongly-typed methods like `ReadTestAsync()` and `ReadConfigAsync()`

Example generated code:
```csharp
namespace YourAssembly
{
    public static class YourAssemblyResourceAccess
    {
        public static class Keys
        {
            public const string test = "test";
            public const string config = "config";
        }
        
        public static ResourcePackageReader Reader { get; }
        
        public static Task<byte[]> ReadTestAsync() { ... }
        public static Task<string> ReadTestAsyncAsString() { ... }
        
        public static Task<byte[]> ReadConfigAsync() { ... }
        public static Task<string> ReadConfigAsyncAsString() { ... }
    }
}
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
