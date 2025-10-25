# NuGet Reference Example

This example demonstrates how to use LuYao.ResourcePacker.MSBuild via NuGet package reference.

**Note**: This example is intended to show the expected project structure when using the NuGet package. To test it, you need to:
1. Pack the NuGet package locally: `dotnet pack LuYao.ResourcePacker.MSBuild/LuYao.ResourcePacker.MSBuild.csproj -o /tmp/local-nuget`
2. Create a `nuget.config` file pointing to your local package source
3. Build this project

Alternatively, use the published NuGet package version once available.

## Project Structure

```
NuGetReferenceExample/
├── NuGetReferenceExample.csproj  # Project file with NuGet package reference
├── Program.cs                     # Application code
├── Resources/
│   ├── config.res.json           # Resource file (JSON)
│   └── message.res.txt           # Resource file (text)
└── README.md
```

## Key Points

1. **NuGet Package References**: The project references both:
   - `LuYao.ResourcePacker`: Core library for reading packed resources at runtime
   - `LuYao.ResourcePacker.MSBuild`: Build-time MSBuild tasks for packing resources

2. **Automatic Build Integration**: The MSBuild targets and props files are automatically imported by NuGet, so you don't need to manually configure any build tasks.

3. **Resource Naming Convention**: Files matching the pattern `*.res.*` are automatically packed into a `.dat` file during build.

4. **Configuration**: You can customize the behavior using MSBuild properties:
   - `ResourcePackerEnabled`: Enable/disable resource packing (default: `true`)
   - `ResourcePackerPattern`: File pattern for resources (default: `*.res.*`)
   - `ResourcePackerOutputFileName`: Output filename (default: `$(AssemblyName).dat`)

## Building (with local NuGet package)

1. Create a `nuget.config` in this directory:
```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="local" value="/tmp/local-nuget" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
</configuration>
```

2. Build:
```bash
dotnet build
```

After building, the `NuGetReferenceExample.dat` file will be automatically generated in the output directory.

## Running

```bash
dotnet run
```

The application will read and display the contents of the packed resources.

## What Makes This Different from ExampleProject?

The main `ExampleProject` uses **project references** to LuYao.ResourcePacker.MSBuild, which is useful for development. This example uses **NuGet package reference**, which is how external users would consume the library.

When using NuGet packages, the MSBuild props and targets files must be named exactly as `<PackageId>.props` and `<PackageId>.targets` to be automatically imported. This example validates that the naming is correct.

For more information about NuGet MSBuild conventions, see the [official documentation on creating MSBuild props/targets packages](https://learn.microsoft.com/en-us/nuget/create-packages/creating-a-package#include-msbuild-props-and-targets-in-a-package).

