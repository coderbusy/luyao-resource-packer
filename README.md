# LuYao.ResourcePacker

[![NuGet Version](https://img.shields.io/nuget/v/LuYao.ResourcePacker)](https://www.nuget.org/packages/LuYao.ResourcePacker/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/LuYao.ResourcePacker)](https://www.nuget.org/packages/LuYao.ResourcePacker/)
[![GitHub Stars](https://img.shields.io/github/stars/coderbusy/luyao-resource-packer?style=social)](https://github.com/coderbusy/luyao-resource-packer/stargazers)

LuYao.ResourcePacker is a .NET library for packaging and accessing resource files during build time and runtime.

## Features

- Pack multiple resource files into a single .dat file during build
- Directory-based resource scanning (default: Resources directory)
- MSBuild integration
- Simple runtime API for resource access
- Async support
- Configurable through MSBuild properties
- **C# Source Generator for strongly-typed resource access with fixed "R" class (Android-style)**

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

Place your resource files in the `Resources` directory:
```
Resources/
  ├── message.json
  ├── config.txt
  └── template.html
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

### 3. Runtime Access - Strongly-Typed API

The source generator automatically creates an internal static class named `R` (Android-style) in the project's root namespace with strongly-typed access to your resources:

```csharp
using LuYao.ResourcePacker;
using YourAssembly; // Import the namespace where the generated class resides

// Access resource keys as constants
Console.WriteLine(R.Keys.message);
Console.WriteLine(R.Keys.config);
Console.WriteLine(R.Keys.template);

// Read resources using generated methods
string message = await R.ReadMessageAsyncAsString();
byte[] configBytes = await R.ReadConfigAsync();
string template = await R.ReadTemplateAsyncAsString();

// Access the underlying reader if needed
ResourcePackageReader reader = R.Reader;
```

**Benefits of the Strongly-Typed API:**
- IntelliSense support for resource names
- Compile-time checking of resource names
- No magic strings in your code
- Auto-generated documentation comments
- Simple, consistent "R" class name across all projects

## Configuration

In your .csproj file:

```xml
<PropertyGroup>
    <!-- Enable/disable resource packing -->
    <ResourcePackerEnabled>true</ResourcePackerEnabled>
    
    <!-- Custom resource directory (default: Resources) -->
    <ResourcePackerDirectory>Resources</ResourcePackerDirectory>
    
    <!-- Custom output filename -->
    <ResourcePackerOutputFileName>$(AssemblyName).dat</ResourcePackerOutputFileName>
</PropertyGroup>
```

## How the Source Generator Works

When you add resource files (e.g., `test.txt`, `config.json`) to your Resources directory:

1. During build, the MSBuild task scans all files in the `Resources` directory
2. These files are packaged into a `.dat` file
3. The source generator creates an internal static class named `R` in the project's root namespace (defaults to assembly name) with:
   - A nested `Keys` class containing const strings for each resource (filename without extension)
   - A static `Reader` property providing access to the `ResourcePackageReader`
   - Strongly-typed methods like `ReadTestAsync()` and `ReadConfigAsync()`
4. Resource keys are generated from filenames (without extension), with invalid C# identifier characters replaced by underscores

Example generated code:
```csharp
namespace YourAssembly
{
    internal static class R
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
